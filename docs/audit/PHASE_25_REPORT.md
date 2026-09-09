# PHASE 25 Report

- Phase: 25 – Reference Image Provider
- Status: **GATED_NOT_INSTALLED** (FLUX/Qwen-Generierung) + **PASS** (Import, Probe, Persistenz)

## Was wirklich existiert (PASS)

- `IReferenceImageGenerationService` + `ReferenceImageService`
- Hardwaretest (`nvidia-smi`, sonst kein erfundenes CUDA)
- Domain `ReferenceSet` / `ReferenceImage` mit prompt, seed, model hash, width/height, SHA256
- Import eines **echten PNG** in das Project-ReferenceSet, Save/Reload im `.3dgod`
- `Probe_IsNotAvailable_AndNeverSuccess` – nie Available, nie „success“

## GATED_NOT_INSTALLED

- Kein FLUX.1-schnell / Qwen-Image-Checkpoint
- Kein CUDA
- `GenerateAsync` ohne Checkpoint wirft **NotInstalled** und schreibt keine Datei
- UI-Button deaktiviert wenn `FeatureAvailability` ≠ invocable

## Gates

- `ReferenceImageTests`, `FeatureAvailabilityTests.GenerativeBackends_*`
