# PHASE 32 Report

- Phase: 32 – PBR Material Editor
- Status: **PASS** (Katalog-PBR: GLB + Viewport) – **kein** KI-Text-to-PBR

## Was wirklich existiert (PASS)

- Presets: Gold, Silver, Steel, Leather, Cloth, Plastic, Skin (Metallic/Roughness + BaseColor).
- Prompt **"Kette Gold, weniger glänzend"** mappt regelbasiert auf Gold + höhere Roughness (`PbrMaterials.FromPrompt`).
- `ApplyToGlb` schreibt Faktoren ins GLB; `ReadFirst` liest sie nach Reload.
- `MaterialEditorPanel`: Katalog-Presets, Metallic/Roughness-Slider, Viewport-Material via `GlbLoader` (GLB-Channels oder Override).
- `FeatureIds.MaterialEditorPbr` = **Available**.

## Nicht vorhanden (ehrlich)

- Kein AI-Text-to-PBR über ein Modell; Mapping ist regelbasiert.
- Kein Texture-Paint / Multi-Material-Slots im Viewport.

## Gates

- `AssetPipelineTests.GoldLessShiny_ChangesRoughness_AndSurvivesGlbReload`
- `FeatureAvailabilityTests.MaterialEditorPbr_IsAvailable`
