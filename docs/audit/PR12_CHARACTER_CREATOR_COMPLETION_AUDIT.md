# PR12 Character Creator completion audit

Scope: headless/CI implementation possible without the user's future high-performance development PC.

## Implemented in PR12

- Human / Orc / Rat Character Creator UI over authoritative ActiveProjectSession.
- AI prompt decomposition to the same Anny phenotype/local parameters used by manual controls.
- Undoable height, muscle, jaw, shoulder, arm and related allow-listed edits.
- Creature add/replace/remove part operations without whole-character regeneration.
- Exact Anny MakeHuman topology target application and persistent multi-target CC0 morph stack.
- CC0 Animal 01 / Bodyparts 01 / Hair 01 / Equipment 01 / Shirts 01 / Suits 02 catalog integration.
- MHCLO parser/fitter for direct and weighted three-reference mappings, reference-axis scaling and fail-closed topology/index validation.
- MHCLO fitting is used for compatible MakeHuman-topology hair/clothing/bodypart assets; incompatible topology is refused rather than raw-attached as fitted.
- Garments, attachments and creature parts persist in .3dgod and participate in composed scene/export.
- Rig structural validation and LBS test-pose UI.
- UE5 preflight + FBX preparation path; editor import is not claimed.
- Freeform catalog paths for dragon, frog humanoid and reptile humanoid; arbitrary generation remains dependent on a real installed Image-to-3D backend.
- Headless E2E: Orc -> edit -> material -> gear -> save/reopen -> rig validation -> pose -> composed GLB -> UE5 preflight.

## Deliberately gated

- Interactive WPF/Helix visual correctness: GATED_INTERACTIVE.
- Actual UE5 editor import: GATED_UE5.
- GPU-only generative backends: GATED_HARDWARE / NotInstalled according to provider probe.
- Artistic quality of third-party assets: not inferred from structural tests.
- Arbitrary AI-created hair/armor/creatures without an installed generative provider: not faked.

## Known limitations

- MHCLO sub-rigs and custom .mhw weight files are not imported by the new standalone fitter. Static fitting is implemented; rig weight transfer continues through existing 3D God garment/rig services.
- MHCLO v120 polygon-reference mapping is not claimed by the v110 three-reference fitter.
- CC0 pack downloads are external network dependencies and therefore are not re-downloaded by every unit-test run.
- The deterministic parser handles a growing allow-list; local-LLM planning still has to pass the same AiEditPlan validator.
- Existing historical audit documents describe earlier repository states and must not override this PR-specific audit.

## Merge rule

Do not merge PR12 until the latest full CI workflow is green. A green structural/headless CI does not convert interactive/GPU/UE5 gates into PASS_REAL.
