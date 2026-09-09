# PHASE 35 Report

- Phase: 35 – Rig Validator / Test Poses
- Status: **PASS**

## Was wirklich existiert

- `HumanoidTestRig.WriteGood`: echtes skinned GLB (18 Joints inkl. `tail.base`, JOINTS_0/WEIGHTS_0, Inverse-Bind).
- `RigValidator`: Skin, Joints, Weight-Summe ≈ 1, Index-Range, finite IBM, Humanoid-Semantik, Hierarchie-Zyklen.
- `TestPoseEvaluator`: Linear-Blend-Skinning für arms, elbow, knee, head, squat, tail – Vertices bewegen sich.
- Known-good besteht; unskinned Mesh fällt mit `NoSkin`; zyklische Bone-Hierarchie fällt.

## Nicht vorhanden

- Kein SkinTokens-generiertes Rig. Der Good-Rig ist ein SharpGLTF-Testasset mit Provenance „authored“.
- Kein Viewport-Pose-Gizmo.

## Gates

- `RigValidatorTests`
