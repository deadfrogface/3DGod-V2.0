#!/usr/bin/env python3
"""3dgod-worker/1 TripoSR Image→3D worker. MIT (TripoSR code + StabilityAI weights)."""
from __future__ import annotations

import json
import os
import sys
import traceback
from pathlib import Path

PROTOCOL = "3dgod-worker/1"
MAX_LINE = 1024 * 1024
WORKER_DIR = Path(__file__).resolve().parent
if str(WORKER_DIR) not in sys.path:
    sys.path.insert(0, str(WORKER_DIR))

_MODEL = None
_DEVICE = None


def send(obj):
    sys.stdout.write(json.dumps(obj, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def error(req_id, code, message, stage="triposr"):
    send(
        {
            "v": PROTOCOL,
            "type": "error",
            "id": req_id,
            "code": code,
            "message": message,
            "diagnostics": {
                "exceptionType": code,
                "traceback": traceback.format_exc() if sys.exc_info()[0] else "",
                "stage": stage,
                "lastSuccessfulStage": "hello",
            },
        }
    )


def resolve_model_dir(params: dict) -> Path:
    explicit = params.get("modelDir") or os.environ.get("3DGOD_TRIPOSR_MODEL_DIR")
    if explicit:
        return Path(explicit)
    if os.name == "nt":
        return Path(os.environ["LOCALAPPDATA"]) / "3DGod" / "Models" / "triposr"
    xdg = os.environ.get("XDG_DATA_HOME")
    base = Path(xdg) if xdg else Path.home() / ".local" / "share"
    return base / "3DGod" / "Models" / "triposr"


def pick_device(requested: str | None) -> str:
    import torch

    req = (requested or "auto").lower()
    if req.startswith("cuda") and torch.cuda.is_available():
        return req if req != "cuda" else "cuda:0"
    if req == "cpu":
        return "cpu"
    return "cuda:0" if torch.cuda.is_available() else "cpu"


def load_model(model_dir: Path, device: str):
    global _MODEL, _DEVICE
    if _MODEL is not None and _DEVICE == device:
        return _MODEL

    config_path = model_dir / "config.yaml"
    weight_path = model_dir / "model.ckpt"
    if not config_path.is_file() or not weight_path.is_file():
        raise RuntimeError(
            "NotInstalled – TripoSR checkpoint missing "
            f"(need {config_path.name} + {weight_path.name} under {model_dir})."
        )

    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        from tsr.system import TSR

        model = TSR.from_pretrained(
            str(model_dir),
            config_name="config.yaml",
            weight_name="model.ckpt",
        )
        model.renderer.set_chunk_size(8192)
        model.to(device)
    finally:
        sys.stdout = real_out

    _MODEL = model
    _DEVICE = device
    return model


def prepare_image(image_path: Path, remove_bg: bool, foreground_ratio: float):
    from PIL import Image

    from tsr.utils import remove_background, resize_foreground

    image = Image.open(image_path)
    if remove_bg:
        real_out = sys.stdout
        sys.stdout = sys.stderr
        try:
            import rembg

            session = rembg.new_session("u2net")
            image = remove_background(image, rembg_session=session)
            image = resize_foreground(image, foreground_ratio)
        finally:
            sys.stdout = real_out
        if image.mode == "RGBA":
            bg = Image.new("RGBA", image.size, (127, 127, 127, 255))
            image = Image.alpha_composite(bg, image).convert("RGB")
        else:
            image = image.convert("RGB")
    else:
        image = image.convert("RGB")
    return image


def generate_mesh(params: dict, progress_id):
    import numpy as np
    import torch
    import trimesh

    image_path = Path(params.get("imagePath") or params["imagePath"])
    glb_path = Path(params.get("glbPath") or params.get("destinationGlb") or params["glbPath"])
    if not image_path.is_file():
        raise RuntimeError(f"ImageNotFound – {image_path}")

    model_dir = resolve_model_dir(params)
    device = pick_device(params.get("device"))
    mc_resolution = int(params.get("mcResolution") or params.get("mcResolution") or (96 if device == "cpu" else 256))
    remove_bg = bool(params.get("removeBackground", params.get("removeBackground", True)))
    foreground_ratio = float(params.get("foregroundRatio") or 0.85)
    has_vertex_color = bool(params.get("vertexColors", params.get("vertexColors", True)))

    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 5, "message": f"loading model on {device}"})
    model = load_model(model_dir, device)

    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 20, "message": "preprocess"})
    image = prepare_image(image_path, remove_bg, foreground_ratio)

    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 40, "message": "infer"})
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        with torch.no_grad():
            scene_codes = model([image], device=device)
            send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 70, "message": "isosurface"})
            meshes = model.extract_mesh(scene_codes, has_vertex_color, resolution=mc_resolution)
    finally:
        sys.stdout = real_out

    if not meshes:
        raise RuntimeError("TripoSR produced no mesh.")
    tri = meshes[0]
    if not isinstance(tri, trimesh.Trimesh):
        raise RuntimeError("TripoSR did not return a trimesh.Trimesh.")

    if tri.vertices is None or len(tri.vertices) < 3 or len(tri.faces) < 1:
        raise RuntimeError("TripoSR mesh empty.")
    if not np.isfinite(tri.vertices).all():
        raise RuntimeError("TripoSR mesh contains NaN/Inf.")

    glb_path.parent.mkdir(parents=True, exist_ok=True)
    # NumPy 2 removed ndarray.ptp used by some trimesh GLB paths; export via GLTF dict safely.
    try:
        tri.export(str(glb_path), file_type="glb")
    except AttributeError:
        import tempfile

        with tempfile.TemporaryDirectory() as td:
            obj_path = Path(td) / "mesh.obj"
            tri.export(str(obj_path), file_type="obj")
            reloaded = trimesh.load(str(obj_path), force="mesh")
            if not isinstance(reloaded, trimesh.Trimesh):
                raise RuntimeError("OBJ reload failed for GLB export.")
            # Drop vertex colors if they trigger exporter bugs.
            reloaded.visual = trimesh.visual.ColorVisuals(mesh=reloaded)
            reloaded.export(str(glb_path), file_type="glb")
    if not glb_path.is_file() or glb_path.stat().st_size < 64:
        raise RuntimeError("GLB export failed.")

    bounds = tri.bounds.tolist() if tri.bounds is not None else None
    return {
        "glbPath": str(glb_path),
        "vertexCount": int(len(tri.vertices)),
        "triangleCount": int(len(tri.faces)),
        "device": device,
        "mcResolution": mc_resolution,
        "bounds": bounds,
        "supportedButSlow": device == "cpu",
    }


def handle_request(msg):
    req_id = msg.get("id")
    method = msg.get("method")
    params = msg.get("params") or {}
    try:
        if method in ("ping", "worker.ping"):
            send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": {"pong": True, "worker": "triposr"}})
            return
        if method in ("image.to3d.probe", "triposr.probe"):
            model_dir = resolve_model_dir(params)
            has_cfg = (model_dir / "config.yaml").is_file()
            has_ckpt = (model_dir / "model.ckpt").is_file()
            import torch

            cuda = torch.cuda.is_available()
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {
                        "modelDir": str(model_dir),
                        "hasConfig": has_cfg,
                        "hasCheckpoint": has_ckpt,
                        "cuda": cuda,
                        "deviceDefault": "cuda:0" if cuda else "cpu",
                    },
                }
            )
            return
        if method in ("image.to3d.generate", "triposr.generate"):
            data = generate_mesh(params, req_id)
            send({"v": PROTOCOL, "type": "progress", "id": req_id, "percent": 100, "message": "done"})
            send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": data})
            return
        error(req_id, "UnknownMethod", f"Unknown method {method}", "request")
    except Exception as exc:  # noqa: BLE001
        code = "NotInstalled" if "NotInstalled" in str(exc) else "PythonException"
        error(req_id, code, str(exc), method or "triposr")


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "triposr", "protocol": PROTOCOL})
    for raw in sys.stdin:
        if len(raw.encode("utf-8")) > MAX_LINE:
            send({"v": PROTOCOL, "type": "error", "code": "OversizedLine", "message": "line too long"})
            continue
        try:
            msg = json.loads(raw)
        except json.JSONDecodeError as exc:
            send({"v": PROTOCOL, "type": "error", "code": "InvalidJson", "message": str(exc)})
            continue
        kind = msg.get("type")
        if kind == "shutdown":
            break
        if kind == "cancel":
            send({"v": PROTOCOL, "type": "error", "id": msg.get("id"), "code": "Cancelled", "message": "cancelled"})
            continue
        if kind == "request":
            handle_request(msg)


if __name__ == "__main__":
    main()
