# PHASE 39 Report

- Phase: 39 – Creature Edits per Text
- Status: **PASS**

## Was wirklich existiert

- Prompts **"Rattenkopf."**, **"Hörner."**, **"rechte Hand mechanisch."** → Allow-List `creature.replacePart` / `creature.addPart` (ReplaceBodyPart / AddCreaturePart).
- Katalog-Meshes (Rattenkopf, Hörner, mechanische Rechte Hand), nicht FLUX/TripoSR.
- Nur der beabsichtigte Slot ändert sich: Kopf-Tausch lässt Hände, Horn-Add lässt Kopf/Hände, Hand-Tausch lässt Kopf/Linke Hand.
- Eine Undo-Transaktion stellt den vorherigen Part wieder her (inkl. MeshAssetId).

## Nicht vorhanden

- Kein generiertes AI-Part (Image-to-3D bleibt NotInstalled). Priority „catalog → generated → R&D“ stoppt hier beim Katalog.

## Gates

- `CreatureTextEditTests` + Interpreter-Corpus inkl. deutscher Prompts
