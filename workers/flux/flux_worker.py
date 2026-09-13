#!/usr/bin/env python3
"""3dgod-worker/1 FLUX.1-schnell Text→Image worker (Apache-2.0 weights only)."""
from __future__ import annotations

import json
import os
import sys
import traceback
from pathlib import Path

PROTOCOL = "3dgod-worker/1"
MAX_LINE = 1024 * 1024
WORKER_DIR = Path(__file__).resolve().parent
HF_REPO = "black-forest-labs/FLUX.1-schnell"
HF_REVISION = "741f7c3ce8b383c54771c7003378a50191e9efe9"
MIN_FREE_BYTES = 40 * 1024 * 1024 * 1024
MIN_VRAM_MB = 8192

_PIPE = None
_DEVICE = None


def send(obj):
    sys.stdout.write(json.dumps(obj, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def error(req_id, code, message, stage="flux"):
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
    explicit = params.get("modelDir") or os.environ.get("THREEDGOD_FLUX_MODEL_DIR")
    if explicit:
        return Path(explicit)
    if os.name == "nt":
        return Path(os.environ["LOCALAPPDATA"]) / "3DGod" / "Models" / "flux"
    xdg = os.environ.get("XDG_DATA_HOME")
    base = Path(xdg) if xdg else Path.home() / ".local" / "share"
    return base / "3DGod" / "Models" / "flux"


def model_ready(model_dir: Path) -> bool:
    if (model_dir / "model_index.json").is_file() and (model_dir / "transformer").is_dir():
        return True
    if (model_dir / "flux1-schnell.safetensors").is_file() and (model_dir / "ae.safetensors").is_file():
        return True
    return False


def free_bytes(path: Path) -> int:
    path.mkdir(parents=True, exist_ok=True)
    usage = os.statvfs(str(path))
    return int(usage.f_bavail * usage.f_frsize)


def cuda_vram_mb() -> int | None:
    try:
        import torch

        if not torch.cuda.is_available():
            return None
        props = torch.cuda.get_device_properties(0)
        return int(props.total_memory // (1024 * 1024))
    except Exception:
        return None


def pick_device(requested: str | None) -> str:
    import torch

    req = (requested or "auto").lower()
    if req == "cpu":
        return "cpu"
    if req.startswith("cuda") and torch.cuda.is_available():
        return "cuda:0" if req == "cuda" else req
    return "cuda:0" if torch.cuda.is_available() else "cpu"


def load_pipeline(model_dir: Path, device: str):
    global _PIPE, _DEVICE
    if _PIPE is not None and _DEVICE == device:
        return _PIPE

    import torch
    from diffusers import FluxPipeline

    dtype = torch.bfloat16 if device.startswith("cuda") else torch.float32
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        if not (model_dir / "model_index.json").is_file():
            raise RuntimeError(
                "NotInstalled – FLUX.1-schnell diffusers snapshot missing "
                f"(need model_index.json under {model_dir})."
            )
        pipe = FluxPipeline.from_pretrained(str(model_dir), torch_dtype=dtype)
        if device.startswith("cuda"):
            pipe = pipe.to(device)
        else:
            pipe.enable_sequential_cpu_offload()
    finally:
        sys.stdout = real_out

    _PIPE = pipe
    _DEVICE = device
    return pipe


def acquire_model(model_dir: Path, progress_id):
    free = free_bytes(model_dir)
    if free < MIN_FREE_BYTES:
        raise RuntimeError(
            f"InsufficientDisk – need >=40GB free for FLUX.1-schnell, have {free // (1024**3)}GB."
        )
    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 5, "message": "acquiring FLUX.1-schnell"})
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        from huggingface_hub import snapshot_download

        snapshot_download(
            repo_id=HF_REPO,
            revision=HF_REVISION,
            local_dir=str(model_dir),
            local_dir_use_symlinks=False,
        )
    finally:
        sys.stdout = real_out
    if not model_ready(model_dir):
        raise RuntimeError("AcquireFailed – snapshot incomplete after download.")
    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 90, "message": "acquire complete"})


def generate_image(params: dict, progress_id):
    prompt = (params.get("prompt") or "").strip()
    if not prompt:
        raise RuntimeError("InvalidRequest – prompt is required.")
    out_path = Path(params.get("outputPath") or params.get("pngPath") or "")
    if not out_path:
        raise RuntimeError("InvalidRequest – outputPath is required.")
    out_path.parent.mkdir(parents=True, exist_ok=True)

    model_dir = resolve_model_dir(params)
    if not model_ready(model_dir):
        raise RuntimeError(
            "NotInstalled – FLUX.1-schnell checkpoint missing. "
            "Acquire via Setup Assistant / flux.acquire (Apache-2.0; HF gated:auto accept once)."
        )

    vram = cuda_vram_mb()
    device = pick_device(params.get("device"))
    if device == "cpu" or vram is None:
        raise RuntimeError(
            f"UnsupportedHardware – FLUX.1-schnell requires NVIDIA CUDA (>= {MIN_VRAM_MB}MB VRAM). "
            "No image will be faked."
        )
    if vram < MIN_VRAM_MB:
        raise RuntimeError(
            f"UnsupportedHardware – VRAM {vram}MB < {MIN_VRAM_MB}MB required for FLUX.1-schnell. "
            "No image will be faked."
        )

    width = int(params.get("width") or 1024)
    height = int(params.get("height") or 1024)
    steps = int(params.get("steps") or 4)
    guidance = float(params.get("guidanceScale") or 0.0)
    seed = params.get("seed")
    cancel_flag = params.get("cancelPath")
    if cancel_flag and Path(cancel_flag).is_file():
        raise RuntimeError("Cancelled – cancel flag present before generate.")

    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 10, "message": f"loading on {device}"})
    pipe = load_pipeline(model_dir, device)
    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 40, "message": "infer"})

    import torch

    generator = None
    if seed is not None:
        generator = torch.Generator(device="cpu").manual_seed(int(seed))

    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        result = pipe(
            prompt,
            width=width,
            height=height,
            num_inference_steps=steps,
            guidance_scale=guidance,
            generator=generator,
            max_sequence_length=256,
        )
        image = result.images[0]
        image.save(out_path, format="PNG")
    finally:
        sys.stdout = real_out

    if cancel_flag and Path(cancel_flag).is_file():
        try:
            out_path.unlink(missing_ok=True)
        except OSError:
            pass
        raise RuntimeError("Cancelled – cancel flag present after generate.")

    if not out_path.is_file() or out_path.stat().st_size < 64:
        raise RuntimeError("GenerateFailed – PNG missing or empty. No placeholder written.")

    from PIL import Image as PILImage

    with PILImage.open(out_path) as im:
        w, h = im.size
        if w <= 0 or h <= 0:
            raise RuntimeError("GenerateFailed – invalid PNG dimensions.")

    return {
        "pngPath": str(out_path),
        "width": int(w),
        "height": int(h),
        "bytes": int(out_path.stat().st_size),
        "seed": int(seed) if seed is not None else None,
        "device": device,
        "modelRepo": HF_REPO,
        "modelRevision": HF_REVISION,
        "steps": steps,
    }


def handle_request(msg):
    req_id = msg.get("id")
    method = msg.get("method")
    params = msg.get("params") or {}
    try:
        if method in ("ping", "worker.ping"):
            send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": {"pong": True, "worker": "flux"}})
            return
        if method in ("text.toimage.probe", "flux.probe"):
            model_dir = resolve_model_dir(params)
            vram = cuda_vram_mb()
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {
                        "modelDir": str(model_dir),
                        "modelReady": model_ready(model_dir),
                        "cudaVramMb": vram,
                        "minVramMb": MIN_VRAM_MB,
                        "hfRepo": HF_REPO,
                        "hfRevision": HF_REVISION,
                        "licenseId": "apache-2.0",
                    },
                }
            )
            return
        if method in ("text.toimage.acquire", "flux.acquire"):
            model_dir = resolve_model_dir(params)
            acquire_model(model_dir, req_id)
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {"modelDir": str(model_dir), "modelReady": True, "revision": HF_REVISION},
                }
            )
            return
        if method in ("text.toimage.generate", "flux.generate"):
            data = generate_image(params, req_id)
            send({"v": PROTOCOL, "type": "progress", "id": req_id, "percent": 100, "message": "done"})
            send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": data})
            return
        error(req_id, "UnknownMethod", f"Unknown method {method}", "request")
    except Exception as exc:  # noqa: BLE001
        msg_text = str(exc)
        code = "PythonException"
        if "NotInstalled" in msg_text:
            code = "NotInstalled"
        elif "UnsupportedHardware" in msg_text:
            code = "UnsupportedHardware"
        elif "InsufficientDisk" in msg_text:
            code = "InsufficientDisk"
        elif "Cancelled" in msg_text:
            code = "Cancelled"
        error(req_id, code, msg_text, method or "flux")


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "flux", "protocol": PROTOCOL})
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
