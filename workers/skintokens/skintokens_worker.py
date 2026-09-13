#!/usr/bin/env python3
"""3dgod-worker/1 SkinTokens mesh auto-root / autorig worker (MIT code + HF weights)."""
from __future__ import annotations

import json
import os
import subprocess
import sys
import traceback
from pathlib import Path

PROTOCOL = "3dgod-worker/1"
MAX_LINE = 1024 * 1024
WORKER_DIR = Path(__file__).resolve().parent
HF_REPO = "VAST-AI/SkinTokens"
HF_REVISION = "79736cad0fd84de384d5eede659b4ebd24effe33"
MIN_VRAM_MB = 14000
ARTICULATION_CKPT = Path(
    "experiments/articulation_xl_quantization_256_token_4/grpo_1400.ckpt"
)
SKIN_VAE_CKPT = Path("experiments/skin_vae_2_10_32768/last.ckpt")
MIN_GLB_BYTES = 64


def send(obj):
    sys.stdout.write(json.dumps(obj, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def error(req_id, code, message, stage="skintokens"):
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
    explicit = params.get("modelDir") or os.environ.get("THREEDGOD_SKINTOKENS_MODEL_DIR")
    if explicit:
        return Path(explicit)
    if os.name == "nt":
        return Path(os.environ["LOCALAPPDATA"]) / "3DGod" / "Models" / "skintokens"
    xdg = os.environ.get("XDG_DATA_HOME")
    base = Path(xdg) if xdg else Path.home() / ".local" / "share"
    return base / "3DGod" / "Models" / "skintokens"


def checkpoint_paths(model_dir: Path) -> tuple[Path, Path]:
    return model_dir / ARTICULATION_CKPT, model_dir / SKIN_VAE_CKPT


def model_ready(model_dir: Path) -> bool:
    art, vae = checkpoint_paths(model_dir)
    return art.is_file() and vae.is_file() and art.stat().st_size > 1024 and vae.stat().st_size > 1024


def cuda_vram_mb() -> int | None:
    try:
        import torch

        if not torch.cuda.is_available():
            return None
        props = torch.cuda.get_device_properties(0)
        return int(props.total_memory // (1024 * 1024))
    except Exception:
        return None


def cancelled(params: dict) -> bool:
    cancel = params.get("cancelPath")
    return bool(cancel) and Path(cancel).is_file()


def find_upstream() -> Path | None:
    """Prefer vendored upstream checkout, else an importable skintokens package root."""
    local = WORKER_DIR / "upstream"
    if (local / "demo.py").is_file() or (local / "skintokens").is_dir():
        return local
    try:
        import skintokens  # type: ignore

        root = Path(getattr(skintokens, "__file__", "") or "").resolve().parent
        if root.is_dir():
            return root
    except Exception:
        pass
    return None


def acquire_model(model_dir: Path, progress_id):
    model_dir.mkdir(parents=True, exist_ok=True)
    send(
        {
            "v": PROTOCOL,
            "type": "progress",
            "id": progress_id,
            "percent": 5,
            "message": f"acquiring {HF_REPO}@{HF_REVISION}",
        }
    )
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
        raise RuntimeError(
            "AcquireFailed – SkinTokens snapshot incomplete "
            f"(need {ARTICULATION_CKPT} and {SKIN_VAE_CKPT} under {model_dir})."
        )
    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 90, "message": "acquire complete"})


def run_upstream_rig(params: dict, model_dir: Path, progress_id) -> dict:
    """Attempt real SkinTokens inference only when upstream code is present. Never fake a GLB."""
    if cancelled(params):
        raise RuntimeError("Cancelled – cancel flag present before rig.")

    source = Path(params.get("meshPath") or params.get("glbPath") or params.get("sourceGlb") or "")
    dest = Path(params.get("destinationGlb") or params.get("outputGlb") or params.get("riggedGlbPath") or "")
    if not source or not source.is_file():
        raise RuntimeError(f"InvalidRequest – source mesh missing: {source}")
    if not dest:
        raise RuntimeError("InvalidRequest – destinationGlb is required.")
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists():
        dest.unlink()

    upstream = find_upstream()
    if upstream is None:
        raise RuntimeError(
            "NotInstalled – SkinTokens worker env + checkpoints + upstream code required. "
            "Place VAST-AI-Research/SkinTokens under workers/skintokens/upstream "
            "(or install an importable skintokens package). No skinned GLB will be faked."
        )

    demo_py = upstream / "demo.py"
    send(
        {
            "v": PROTOCOL,
            "type": "progress",
            "id": progress_id,
            "percent": 20,
            "message": f"running SkinTokens upstream from {upstream}",
        }
    )

    env = os.environ.copy()
    env["SKINTOKENS_MODEL_DIR"] = str(model_dir)
    env["PYTHONPATH"] = str(upstream) + os.pathsep + env.get("PYTHONPATH", "")

    if demo_py.is_file():
        cmd = [
            sys.executable,
            str(demo_py),
            "--input",
            str(source),
            "--output",
            str(dest),
        ]
        if params.get("useTransfer"):
            cmd.append("--use_transfer")
        if params.get("useSkeleton"):
            cmd.append("--use_skeleton")
        real_out = sys.stdout
        sys.stdout = sys.stderr
        try:
            proc = subprocess.run(
                cmd,
                cwd=str(upstream),
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )
        finally:
            sys.stdout = real_out
        if cancelled(params):
            if dest.exists():
                try:
                    dest.unlink()
                except OSError:
                    pass
            raise RuntimeError("Cancelled – cancel flag present after rig.")
        if proc.returncode != 0:
            detail = (proc.stderr or proc.stdout or "").strip()[-2000:]
            raise RuntimeError(
                f"RigFailed – SkinTokens upstream demo.py exited {proc.returncode}. {detail}"
            )
    else:
        # Import-path fallback: expect upstream to expose a callable entry.
        if str(upstream) not in sys.path:
            sys.path.insert(0, str(upstream))
        try:
            import skintokens  # type: ignore

            if not hasattr(skintokens, "rig_mesh"):
                raise RuntimeError(
                    "NotInstalled – skintokens package importable but has no rig_mesh(); "
                    "vendor demo.py under workers/skintokens/upstream. No skinned GLB will be faked."
                )
            send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 40, "message": "infer"})
            skintokens.rig_mesh(str(source), str(dest), model_dir=str(model_dir))
        except ImportError as exc:
            raise RuntimeError(
                "NotInstalled – SkinTokens upstream code not runnable "
                f"({exc}). Vendor upstream under workers/skintokens/upstream. "
                "No skinned GLB will be faked."
            ) from exc

    if cancelled(params):
        if dest.exists():
            try:
                dest.unlink()
            except OSError:
                pass
        raise RuntimeError("Cancelled – cancel flag present after rig.")

    if not dest.is_file() or dest.stat().st_size < MIN_GLB_BYTES:
        if dest.exists():
            try:
                dest.unlink()
            except OSError:
                pass
        raise RuntimeError(
            "RigFailed – output GLB missing or trivial size. No placeholder/fake skinned GLB written."
        )

    return {
        "glbPath": str(dest),
        "bytes": int(dest.stat().st_size),
        "modelDir": str(model_dir),
        "hfRepo": HF_REPO,
        "hfRevision": HF_REVISION,
        "upstream": str(upstream),
    }


def rig_mesh(params: dict, progress_id) -> dict:
    model_dir = resolve_model_dir(params)
    if not model_ready(model_dir):
        raise RuntimeError(
            "NotInstalled – SkinTokens checkpoints missing "
            f"(need {ARTICULATION_CKPT} and {SKIN_VAE_CKPT} under {model_dir}). "
            "Acquire via skintokens.acquire. No skinned GLB will be faked."
        )

    vram = cuda_vram_mb()
    if vram is None:
        raise RuntimeError(
            f"UnsupportedHardware – SkinTokens requires NVIDIA CUDA (>= {MIN_VRAM_MB}MB VRAM). "
            "No skinned GLB will be faked."
        )
    if vram < MIN_VRAM_MB:
        raise RuntimeError(
            f"UnsupportedHardware – VRAM {vram}MB < {MIN_VRAM_MB}MB required for SkinTokens. "
            "No skinned GLB will be faked."
        )

    send({"v": PROTOCOL, "type": "progress", "id": progress_id, "percent": 10, "message": f"cuda VRAM {vram}MB"})
    return run_upstream_rig(params, model_dir, progress_id)


def handle_request(msg):
    req_id = msg.get("id")
    method = msg.get("method")
    params = msg.get("params") or {}
    try:
        if method in ("ping", "worker.ping"):
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {"pong": True, "worker": "skintokens"},
                }
            )
            return
        if method in ("mesh.autoroot.probe", "skintokens.probe"):
            model_dir = resolve_model_dir(params)
            vram = cuda_vram_mb()
            upstream = find_upstream()
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
                        "licenseId": "mit",
                        "upstreamPresent": upstream is not None,
                        "upstreamPath": str(upstream) if upstream else None,
                        "classification": "IMPLEMENTED_GATED_HARDWARE",
                    },
                }
            )
            return
        if method in ("mesh.autoroot.acquire", "skintokens.acquire"):
            model_dir = resolve_model_dir(params)
            acquire_model(model_dir, req_id)
            send(
                {
                    "v": PROTOCOL,
                    "type": "result",
                    "id": req_id,
                    "ok": True,
                    "data": {
                        "modelDir": str(model_dir),
                        "modelReady": True,
                        "revision": HF_REVISION,
                    },
                }
            )
            return
        if method in ("mesh.autoroot.rig", "skintokens.rig"):
            data = rig_mesh(params, req_id)
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
        elif "Cancelled" in msg_text:
            code = "Cancelled"
        elif "InvalidRequest" in msg_text:
            code = "InvalidRequest"
        error(req_id, code, msg_text, method or "skintokens")


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "skintokens", "protocol": PROTOCOL})
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
