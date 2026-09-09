# PHASE 03 Report

- Phase: 03 – Legacy Blender entkoppeln
- Status: **PASS** (audit repair)
- Date: 2026-09-09

## Audit repair

- Removed misleading `LaunchAutoRig() => LaunchSculpt()`.
- `LaunchAutoRig` now reports **NotImplemented** via `OnBlenderFailed` and never starts Sculpt.
- Rigging UI already keeps Auto-Rig disabled (`FeatureIds.RigAuto = NotImplemented`).
- Froggy no longer claims Auto-Rig needs Blender.

## Prior DONE (still true)

- `LegacyBlenderBackend` behind `IBlenderOperations`
- Character Core has no Blender class reference
- Headless only (`--background`, `CreateNoWindow`)

## Tests

- `LaunchAutoRig_DoesNotDelegateToSculpt_AndReportsNotImplemented`
- Headless smoke uses explicit `GATED_NOT_INSTALLED` when Blender missing (no silent skip)
