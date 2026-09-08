# PHASE 25 Report

- Phase: 25 – Reference Image Provider
- Status: **PASS** (ehrlicher Gate, kein Fake-FLUX-Bild)

## Was wirklich existiert

- `IReferenceImageGenerationService` + `ReferenceImageService`
- Hardwaretest (`nvidia-smi`, sonst kein erfundenes CUDA)
- FLUX/Qwen-Probe unter `%LOCALAPPDATA%/3DGod/Models/{flux|qwen}` (Checkpoint > 1 MB)
- Domain `ReferenceSet` / `ReferenceImage` mit prompt, seed, model hash, width/height, SHA256
- Import eines **echten PNG** in das Project-ReferenceSet, Save/Reload im `.3dgod`
- `GenerateAsync` ohne Checkpoint wirft **NotInstalled** und schreibt keine Datei

## Nicht vorhanden

- Kein FLUX.1-schnell / Qwen-Image-Checkpoint
- Kein CUDA
- Text→Bild ist daher **nicht** als Generierung erfüllt

## Gates

- `dotnet test -c Release`: 143 bestanden
