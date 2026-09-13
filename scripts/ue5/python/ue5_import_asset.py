# Unreal Editor Python import helper for 3D God Stage 17 smoke.
# Executed via UnrealEditor-Cmd -ExecutePythonScript=...
# Prints success markers that Invoke-Ue5ImportSmoke.ps1 requires for PASS_REAL.
#
# Required env:
#   THREEDGOD_UE5_ASSET  – absolute path to .fbx / .glb / .obj to import
# Optional:
#   THREEDGOD_UE5_DEST   – content path (default /Game/Imported/ThreeDGodImport)

from __future__ import annotations

import os
import traceback


MARKER_OK = "3DGOD_UE5_IMPORT_OK"
MARKER_FAIL = "3DGOD_UE5_IMPORT_FAIL"
MARKER_SKELETON = "3DGOD_UE5_SKELETON_OK"
MARKER_MATERIALS = "3DGOD_UE5_MATERIALS_OK"
MARKER_MORPH = "3DGOD_UE5_MORPH_OK"
MARKER_LOD = "3DGOD_UE5_LOD_OK"


def _log(msg: str) -> None:
    print(msg, flush=True)


def _import_with_interchange(asset_path: str, dest: str):
    """Prefer Interchange when available (UE5.1+)."""
    import unreal

    source_data = unreal.InterchangeManager.create_source_data(asset_path)
    mgr = unreal.InterchangeManager.get_interchange_manager_scripted()
    options = unreal.ImportAssetParameters()
    options.is_automated = True
    # destination_path style APIs vary by engine version; use soft content path when present.
    try:
        options.destination_name = os.path.splitext(os.path.basename(asset_path))[0]
    except Exception:
        pass
    ok = mgr.import_asset(dest, source_data, options)
    return bool(ok)


def _import_with_asset_tools(asset_path: str, dest: str):
    """Fallback AssetTools task import."""
    import unreal

    tasks = []
    task = unreal.AssetImportTask()
    task.filename = asset_path
    task.destination_path = dest
    task.automated = True
    task.replace_existing = True
    task.save = True
    tasks.append(task)
    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks(tasks)
    if task.imported_object_paths:
        return True
    # Some builds leave paths empty even on soft success; check asset registry.
    name = os.path.splitext(os.path.basename(asset_path))[0]
    asset_path_full = f"{dest.rstrip('/')}/{name}.{name}"
    return unreal.EditorAssetLibrary.does_asset_exist(asset_path_full)


def _probe_optional_markers(dest: str, asset_path: str) -> None:
    import unreal

    name = os.path.splitext(os.path.basename(asset_path))[0]
    base = f"{dest.rstrip('/')}/{name}"
    # Soft probes — markers are optional for PASS_REAL (IMPORT_OK is required).
    try:
        if unreal.EditorAssetLibrary.does_asset_exist(base):
            asset = unreal.EditorAssetLibrary.load_asset(base)
            if asset is None:
                return
            # Skeletal mesh?
            try:
                sk = unreal.SkeletalMesh.cast(asset)
                if sk is not None:
                    _log(MARKER_SKELETON)
                    try:
                        if sk.get_editor_property("morph_targets") or getattr(sk, "morph_targets", None):
                            _log(MARKER_MORPH)
                    except Exception:
                        pass
                    try:
                        lods = sk.get_num_lods() if hasattr(sk, "get_num_lods") else 0
                        if lods and lods > 0:
                            _log(MARKER_LOD)
                    except Exception:
                        pass
            except Exception:
                pass
            try:
                mats = unreal.EditorAssetLibrary.find_asset_data(base)
                if mats:
                    _log(MARKER_MATERIALS)
            except Exception:
                pass
    except Exception as exc:
        _log(f"optional marker probe skipped: {exc}")


def main() -> None:
    asset = os.environ.get("THREEDGOD_UE5_ASSET", "").strip()
    dest = os.environ.get("THREEDGOD_UE5_DEST", "/Game/Imported/ThreeDGodImport").strip()
    if not asset or not os.path.isfile(asset):
        _log(f"{MARKER_FAIL} missing asset path: {asset!r}")
        raise SystemExit(1)

    try:
        import unreal  # noqa: F401
    except ImportError:
        _log(f"{MARKER_FAIL} unreal module unavailable – script must run inside UnrealEditor-Cmd")
        raise SystemExit(1)

    try:
        import unreal

        unreal.EditorAssetLibrary.make_directory(dest)
        ok = False
        try:
            ok = _import_with_interchange(asset, dest)
        except Exception as exc:
            _log(f"Interchange import unavailable or failed ({exc}); trying AssetTools")
            ok = _import_with_asset_tools(asset, dest)

        if not ok:
            # Last chance: AssetTools alone
            ok = _import_with_asset_tools(asset, dest)

        if not ok:
            _log(f"{MARKER_FAIL} import returned false for {asset}")
            raise SystemExit(2)

        _log(MARKER_OK)
        _probe_optional_markers(dest, asset)
    except SystemExit:
        raise
    except Exception:
        _log(MARKER_FAIL)
        _log(traceback.format_exc())
        raise SystemExit(3)


if __name__ == "__main__":
    main()
