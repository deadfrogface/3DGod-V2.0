#!/usr/bin/env python3
"""3dgod-worker/1 GarmentCode worker.

PyPI pygarment 2.0.x installs `garmentcode` and `pattern` as top-level packages,
while library internals still import `pygarment.*`. This worker installs a shim
so Panel/Edge assembly works.

Pattern import only — no Warp drape, no CGAL/libigl simulation, no Cairo SVG.
"""
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


def error(req_id, code, message, stage="garmentcode"):
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


def _jsonable(obj):
    if isinstance(obj, dict):
        return {str(k): _jsonable(v) for k, v in obj.items()}
    if isinstance(obj, (list, tuple)):
        return [_jsonable(v) for v in obj]
    if hasattr(obj, "tolist"):
        return _jsonable(obj.tolist())
    if hasattr(obj, "item"):
        try:
            return _jsonable(obj.item())
        except Exception:  # noqa: BLE001
            pass
    if isinstance(obj, (str, int, float, bool)) or obj is None:
        return obj
    return str(obj)


def _site_packages_with_garmentcode() -> str:
    for entry in sys.path:
        root = Path(entry)
        if (root / "garmentcode" / "panel.py").is_file() and (root / "pattern" / "core.py").is_file():
            return str(root)
    raise RuntimeError("NotInstalled – pygarment (garmentcode/pattern) not on PYTHONPATH.")


def install_pygarment_shim():
    """PyPI layout is site-packages/{garmentcode,pattern}/ but internals import pygarment.*.

    Point a synthetic pygarment package at site-packages so `pygarment.garmentcode`
    loads those folders as one module graph (avoids duplicate Edge classes).
    """
    import types

    existing = sys.modules.get("pygarment")
    if existing is not None and getattr(existing, "_3dgod_shim", False):
        return

    for key in list(sys.modules):
        if key == "garmentcode" or key.startswith("garmentcode.") or key == "pattern" or key.startswith("pattern."):
            del sys.modules[key]

    pyg = types.ModuleType("pygarment")
    pyg.__path__ = [_site_packages_with_garmentcode()]  # type: ignore[attr-defined]
    pyg._3dgod_shim = True
    sys.modules["pygarment"] = pyg


def load_pygarment():
    real_out = sys.stdout
    sys.stdout = sys.stderr
    try:
        install_pygarment_shim()
        from pygarment.garmentcode.edge import Edge  # noqa: F401
        from pygarment.garmentcode.panel import Panel  # noqa: F401
        from pygarment.pattern.core import BasicPattern  # noqa: F401
    except Exception as exc:  # noqa: BLE001
        raise RuntimeError(f"NotInstalled – pygarment import failed: {exc}") from exc
    finally:
        sys.stdout = real_out


def _rect_panel(name: str, width: float, height: float, translation):
    from pygarment.garmentcode.edge import Edge
    from pygarment.garmentcode.panel import Panel

    panel = Panel(name)
    v0, v1, v2, v3 = [0.0, 0.0], [width, 0.0], [width, height], [0.0, height]
    panel.edges.append(Edge(v0, v1))
    panel.edges.append(Edge(v1, v2))
    panel.edges.append(Edge(v2, v3))
    panel.edges.append(Edge(v3, v0))
    panel.translate_to(list(translation))
    return panel


def build_jacket(sleeve_cm: float, length_cm: float, width_cm: float):
    """Four-panel jacket via pygarment Panel/Edge. Not Warp-simulated."""
    half = width_cm / 2.0
    sleeve_w = max(12.0, width_cm * 0.35)
    return [
        _rect_panel("jacket-front", width_cm, length_cm, [0.0, length_cm / 2.0, half]),
        _rect_panel("jacket-back", width_cm, length_cm, [0.0, length_cm / 2.0, -half]),
        _rect_panel("jacket-sleeve-L", sleeve_cm, sleeve_w, [-sleeve_cm, length_cm * 0.7, 0.0]),
        _rect_panel("jacket-sleeve-R", sleeve_cm, sleeve_w, [sleeve_cm, length_cm * 0.7, 0.0]),
    ]


def assemble_pattern(panels):
    from pygarment.pattern.core import BasicPattern

    merged = BasicPattern()
    merged.name = "jacket"
    merged.pattern.setdefault("panels", {})
    merged.pattern.setdefault("stitches", [])
    for panel in panels:
        spattern = panel.assembly()
        merged.pattern["panels"].update(spattern.pattern.get("panels") or {})
        stitches = spattern.pattern.get("stitches") or []
        if stitches:
            merged.pattern["stitches"].extend(stitches)
    return merged


def write_obj_from_pattern(pattern, obj_path: Path):
    """Import sewing panels as a 3D mesh (pattern vertices, not Warp draping)."""
    from scipy.spatial.transform import Rotation as R

    panels = pattern.pattern.get("panels") or {}
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, int, int]] = []
    for panel in panels.values():
        raw = panel.get("vertices") or []
        if len(raw) < 3:
            continue
        base = len(verts)
        trans = panel.get("translation") or [0, 0, 0]
        rot = R.from_euler("XYZ", panel.get("rotation") or [0, 0, 0], degrees=True)
        for v in raw:
            p2 = [float(v[0]), float(v[1]), 0.0]
            p3 = rot.apply(p2) + trans
            verts.append((float(p3[0]) * 0.01, float(p3[1]) * 0.01, float(p3[2]) * 0.01))
        for i in range(1, len(raw) - 1):
            faces.append((base + 1, base + i + 1, base + i + 2))
    if len(faces) < 1:
        raise RuntimeError("GarmentCode produced no triangulated panels.")
    obj_path.parent.mkdir(parents=True, exist_ok=True)
    with obj_path.open("w", encoding="utf-8") as f:
        f.write("# pygarment jacket panels (MIT). Not Warp-simulated.\n")
        for x, y, z in verts:
            f.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for a, b, c in faces:
            f.write(f"f {a} {b} {c}\n")
    return len(verts), len(faces)


def generate_jacket(params: dict) -> dict:
    load_pygarment()
    sleeve = float(params.get("sleeveLengthCm", 45.0))
    length = float(params.get("lengthCm", 60.0))
    width = float(params.get("widthCm", 40.0))
    dest = Path(params["objPath"])
    pattern_path = dest.with_suffix(".json")
    panels = build_jacket(sleeve, length, width)
    pattern = assemble_pattern(panels)
    payload = _jsonable(pattern.pattern)
    panel_count = len(payload.get("panels") or {})
    if panel_count < 4:
        raise RuntimeError(f"GarmentCode jacket must have >=4 panels, got {panel_count}.")
    pattern_path.parent.mkdir(parents=True, exist_ok=True)
    pattern_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    vcount, fcount = write_obj_from_pattern(pattern, dest)
    return {
        "objPath": str(dest),
        "patternPath": str(pattern_path),
        "panelCount": panel_count,
        "vertexCount": vcount,
        "triangleCount": fcount,
        "backend": "pygarment",
        "simulated": False,
    }


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "garmentcode"})
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
        if kind != "request":
            continue
        req_id = msg.get("id")
        method = msg.get("method")
        params = msg.get("params") or {}
        try:
            if method == "garment.probe":
                load_pygarment()
                send(
                    {
                        "v": PROTOCOL,
                        "type": "result",
                        "id": req_id,
                        "ok": True,
                        "data": {"backendId": "garmentcode", "license": "MIT", "simBundled": False},
                    }
                )
                continue
            if method == "garment.jacket":
                data = generate_jacket(params)
                send({"v": PROTOCOL, "type": "result", "id": req_id, "ok": True, "data": data})
                continue
            error(req_id, "Unsupported", f"Unknown method {method}", "dispatch")
        except Exception as exc:  # noqa: BLE001
            error(req_id, "PythonException", str(exc), method or "garmentcode")


if __name__ == "__main__":
    main()
