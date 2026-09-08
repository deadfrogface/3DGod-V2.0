# PHASE 32 Report

- Phase: 32 – PBR Material Editor
- Status: **PASS**

## Was wirklich existiert

- Presets: Gold, Silver, Steel, Leather, Cloth, Plastic, Skin (Metallic/Roughness + BaseColor).
- Prompt **"Kette Gold, weniger glänzend"** mappt auf Gold und erhöht Roughness.
- `ApplyToGlb` schreibt Faktoren ins GLB; `ReadFirst` liest sie nach Reload.
- `MaterialDefinition` überlebt `DomainJson` Roundtrip.

## Nicht vorhanden

- Kein interaktiver Viewport-Material-Editor / Texture-Paint.
- Kein AI-Text-to-PBR über ein Modell; Mapping ist regelbasiert.

## Gates

- `AssetPipelineTests.GoldLessShiny_ChangesRoughness_AndSurvivesGlbReload`
