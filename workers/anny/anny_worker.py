#!/usr/bin/env python3
"""3dgod-worker/1 Anny human worker. Apache-2.0 / CC0 paths only. No SMPL-X."""
from __future__ import annotations

import json
import sys
import traceback
from pathlib import Path

PROTOCOL = "3dgod-worker/1"
MAX_LINE = 1024 * 1024


def send(obj):
    sys.stdout.write(json.dumps(obj, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def error(req_id, code, message, stage="anny"):
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


def _to_numpy(value):
    if value is None:
        return None
    if hasattr(value, "detach"):
        value = value.detach().cpu().numpy()
    import numpy as np

    return np.asarray(value)


def load_anny():
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        import anny  # type: ignore
    except Exception as exc:  # noqa: BLE001
        raise RuntimeError(f"NotInstalled – anny import failed: {exc}") from exc
    finally:
        sys.stdout = real_out
    return anny


def catalog(anny):
    model = anny.Anny()  # default topology="anny"; never SMPL-X
    labels = list(getattr(model, "phenotype_labels", []) or [])
    bones = list(getattr(model, "bone_labels", []) or [])
    return {
        "backendId": "anny",
        "backendVersion": getattr(anny, "__version__", "unknown"),
        "topology": "anny",
        "license": "Apache-2.0+CC0",
        "phenotypeKeys": labels,
        "boneLabels": bones,
        "count": len(labels),
    }


def generate_obj(anny, params, dest: Path):
    import torch

    model = anny.Anny()
    labels = list(getattr(model, "phenotype_labels", []) or [])
    kwargs = {}
    for key in labels:
        if key in params:
            kwargs[key] = float(params[key])
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        with torch.no_grad():
            output = model(phenotype_kwargs=kwargs) if kwargs else model()
    finally:
        sys.stdout = real_out
    if not isinstance(output, dict) or "vertices" not in output:
        raise RuntimeError("Anny produced no vertices.")
    verts = _to_numpy(output["vertices"])
    if verts is None:
        raise RuntimeError("Anny produced no vertices.")
    if verts.ndim == 3:
        verts = verts[0]
    faces = _to_numpy(model.faces)
    if faces is None or len(faces) == 0:
        raise RuntimeError("Anny produced no faces.")
    dest.parent.mkdir(parents=True, exist_ok=True)
    with dest.open("w", encoding="utf-8") as handle:
        for v in verts:
            handle.write(f"v {float(v[0]):.6f} {float(v[1]):.6f} {float(v[2]):.6f}\n")
        for f in faces:
            handle.write(f"f {int(f[0]) + 1} {int(f[1]) + 1} {int(f[2]) + 1}\n")
    return int(len(verts)), int(len(faces))


def handle_request(msg):
    req_id = msg.get("id")
    method = msg.get("method")
    params = msg.get("params") or {}
    try:
        anny = load_anny()
        if method == "human.catalog":
            send({"v": PROTOCOL, "type": "progress", "id": req_id, "percent": 50})
            send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": catalog(anny)})
            return
        if method == "human.generate":
            dest = Path(params.get("objPath") or (Path.cwd() / "anny_human.obj"))
            send({"v": PROTOCOL, "type": "progress", "id": req_id, "percent": 20})
            vcount, fcount = generate_obj(anny, params.get("phenotypes") or {}, dest)
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {
                        "objPath": str(dest),
                        "vertexCount": vcount,
                        "triangleCount": fcount,
                    },
                }
            )
            return
        if method == "human.get_rig":
            model = anny.Anny()
            bones = [str(x) for x in (getattr(model, "bone_labels", None) or [])]
            if not bones:
                error(req_id, "NotImplemented", "Anny default topology exposes no verified bone_labels.", "human.get_rig")
                return
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {"bones": bones, "boneCount": len(bones), "skinned": False},
                }
            )
            return
        if method == "human.export_glb":
            error(
                req_id,
                "NotImplemented",
                "GLB is written by the C# SharpGLTF pipeline from the Anny OBJ. Worker does not fake a GLB.",
                "human.export_glb",
            )
            return
        error(req_id, "UnknownMethod", f"Unknown method {method}", "request")
    except Exception as exc:  # noqa: BLE001
        code = "NotInstalled" if "NotInstalled" in str(exc) else "PythonException"
        error(req_id, code, str(exc), method or "anny")


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "anny", "protocol": PROTOCOL})
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
