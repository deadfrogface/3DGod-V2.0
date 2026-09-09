# PHASE 27 Report

- Phase: 27 – Stable Fast 3D
- Status: **GATED_NOT_INSTALLED** (SF3D-Output) + **PASS** (License-/Hardware-Gate, Router)

## Was wirklich existiert (PASS)

- Stability Community License muss in `license.json` akzeptiert sein (`id=stability-community`, `accepted=true`)
- Ohne Acceptance: **LicenseBlocked**, kein Mesh
- Profile `default` (8 GB) und `cpu-fallback`
- Router schließt SF3D ohne License/VRAM/CUDA aus
- `ImageTo3DTests.Probe_IsNotAvailable_AndNeverSuccess("sf3d")`

## GATED_NOT_INSTALLED

- Kein SF3D-Checkpoint, daher kein UV/Material-Output aus Foto

## Gates

- Probe nie Available, nie „success“
