# PR 12 — Character Creator Core

## Goal

Turn the existing backend collection into one deterministic character model that can later drive both sliders and AI prompts.

The first implementation intentionally does **not** add fake geometry generation. It introduces the product-level state required for:

- Human / Humanoid Creature / Freeform modes
- morph stacks
- hair, clothing, accessories and creature-part slots
- appearance properties
- deterministic presets
- AI and manual editing through the same state layer

## First vertical slice

The initial built-in presets are:

- `human` — Anny-backed
- `orc` — Anny-backed humanoid creature with deterministic morph values, grey/green skin intent and two tusk attachment slots

This is state/model infrastructure only. A preset is not claimed to be visually complete until its morph and asset providers have real licensed data and viewport integration.

## Reuse policy

Priority: REUSE > ADAPT > WRAP > PORT > SELF-WRITE.

Default bundled assets must be CC0, MIT or Apache-compatible. GPL/AGPL implementation code must not be copied into the proprietary core. CC-BY content requires explicit attribution and remains optional until reviewed.

Planned reuse audits:

- Anny — existing human provider
- MakeHuman CC0 target/asset data — candidate morph/body-part library
- Configura — character-creator architecture reference/portable permissive pieces after license verification
- Godot3DCharacterEditorWardrobe — wardrobe/slot architecture reference after license verification
- M3 CharacterStudio — mesh merge/atlas/culling algorithms after license verification
- GarmentCode — existing garment generation/fitting
- skin-tokens.cpp — existing auto-rig
- LLamaSharp — existing AI command interpretation

## Hard rule

AI does not directly mutate meshes. AI produces validated edit intent that is applied through the same CharacterCreatorService used by manual controls. Generative geometry remains a separate explicit operation.
