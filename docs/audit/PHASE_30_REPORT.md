# PHASE 30 Report

- Phase: 30 – AI Asset Generation
- Status: **PASS** (Katalog-Fallback real; FLUX/TripoSR bleiben NotInstalled)

## Was wirklich existiert

- `IAssetGenerationService` / `AiAssetPipeline`: Text → ReferenceImage (FLUX fail) → ImageTo3D (TripoSR fail) → **procedural-catalog**.
- Prompt **"Silberne Halskette mit Froschanhänger"** erzeugt echtes Jewelry-GLB (>200 Vertices/Dreiecke), gültiges Preview-PNG und Provenance `backendId=procedural-catalog`.
- `AssetLibrary` unter konfigurierbarem Root (`%LOCALAPPDATA%/3DGod/Library` in der App).
- Unbekannte Prompts: **NotInstalled**, keine Mesh-Datei.

## Nicht vorhanden

- Kein FLUX/Qwen-Bild, kein TripoSR-Mesh. Das Asset ist **kein** KI-generiertes Foto→3D.
- Nur der Frog-Necklace-Prompt liegt im Katalog; andere Kategorien (Weapon/Prop/…) haben noch keinen Generator.

## Gates

- `AssetPipelineTests.FrogNecklacePrompt_CreatesRealAssetWithProvenanceAndPreview`
- `AssetPipelineTests.UnknownPrompt_DoesNotWriteMesh`
