# PHASE 30 Report

- Phase: 30 – AI Asset Generation
- Status: **PASS** (procedural-catalog) + **GATED_NOT_INSTALLED** (FLUX / TripoSR / echte KI-Pipeline)

## Was wirklich existiert (PASS – Katalog, nicht KI)

- `IAssetGenerationService` / `AiAssetPipeline`: Text → ReferenceImage (FLUX fail) → ImageTo3D (TripoSR fail) → **procedural-catalog**.
- Prompt **"Silberne Halskette mit Froschanhänger"** erzeugt echtes Jewelry-GLB (>200 Vertices/Dreiecke), gültiges Preview-PNG und Provenance `backendId=procedural-catalog`.
- `AssetLibrary` unter konfigurierbarem Root.
- Unbekannte Prompts: **NotInstalled**, keine Mesh-Datei.
- UI: `AiGenerateAsset` = **Experimental** mit Hinweis „procedural catalog“.

## GATED_NOT_INSTALLED (echte KI)

- Kein FLUX/Qwen-Bild, kein TripoSR-Mesh.
- Das Asset ist **kein** KI-generiertes Foto→3D.
- Nur der Frog-Necklace-Prompt liegt im Katalog.

## Gates

- `AssetPipelineTests.FrogNecklacePrompt_*`, `UnknownPrompt_DoesNotWriteMesh`
