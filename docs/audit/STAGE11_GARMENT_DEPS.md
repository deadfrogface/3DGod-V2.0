# Stage 11 — GarmentCode dependency posture

Production worker `workers/garmentcode/pyproject.toml` depends only on `pygarment>=2.0.0`.

Transitive lock may still pull CGAL/libigl/NiceGUI via pygarment. Headless 3D God path must not import NiceGUI UI.

Next prune step (when re-locking): verify each transitive package against `garmentcode_worker.py` imports; drop unused extras only after `CiRuntimeIntegrationTests.GarmentCode_CpuJacket_WritesRealGlb` stays green.
