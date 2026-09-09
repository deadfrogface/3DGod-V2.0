# PHASE 31 Report

- Phase: 31 – Semantic Attachments
- Status: **PASS**

## Was wirklich existiert

- Semantische Slots: `neck`, `chest`, `ear.L/R`, `nose`, `wrist.L/R`, `hand.L/R`, `hips`.
- Defaults: Necklace→`neck`, Earring→`ear.L`, Weapon→`hand.R`.
- `AttachmentKinematics.WorldOnPose`: Kette folgt der Neck-Pose (Y-Rotation ändert World-X/Z, Y bleibt).

## Nicht vorhanden

- Kein Live-Viewport-Attachment an einem Anny-Rig in der UI.
- Keine Kollision / Skinning der Kette.

## Gates

- `AssetPipelineTests.Necklace_FollowsNeckOnTestPose`
