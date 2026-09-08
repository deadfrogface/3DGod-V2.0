# PHASE 37 Report

- Phase: 37 – Erster Ork
- Status: **PASS**

## Was wirklich existiert

- `CreateOrc`: humanoider Körper als **skinned** Test-GLB, plus echte Ear/Tusk-Meshes (nicht nur Name/Farbe).
- Anny-Phenotypen gespeichert (`muscle` 0.92, `weight`, `proportions`) – bereit für Worker, kein Fake-Anny-Mesh ohne Runtime.
- Material **OrcSkin** (grün-grau) ≠ Human-Skin-Preset.
- Hauer-Geometrie ≠ Human-Horn (andere Triangle-Counts, kein Uniform-Scale).
- Humanoid-Rig besteht `RigValidator`. Undo entfernt Extra-Parts; `.3dgod` Save/Reload behält Ork.

## Nicht vorhanden

- Kein frisch generiertes Anny-Ork-Gesicht in diesem Gate (Worker wäre Minuten). Phenotypen sind persistiert, Mesh-Unterschied kommt von Parts + Material + Rig.

## Gates

- `OrcCreatureTests.Orc_HasDistinctPartsMaterialRig_UndoAndSave`
