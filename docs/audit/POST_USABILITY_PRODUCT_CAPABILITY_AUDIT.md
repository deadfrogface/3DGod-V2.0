# POST USABILITY PRODUCT CAPABILITY AUDIT

| Field | Value |
|-------|-------|
| Repo | https://github.com/deadfrogface/3DGod-V2.0 |
| Prior audit | PR #9 / `docs/audit/FINAL_PRODUCT_CAPABILITY_AUDIT.md` |
| Baseline SHA | `68fe72f1152a417c9f27d0acb00538eb09cddedd` |
| Implementation tip | See branch `cursor/make-product-usable-b322` |
| Audit date | 2026-09-15 |
| Method | Source re-trace after P0/P1 wiring + headless Stage 27 proofs |

---

## Executive verdict

**Improved, not finished.** 3D God now has **one authoritative ActiveProjectSession** over `ProjectBundle` / `.3dgod` (with embedded mesh bytes, materials, garments, Anny state, autosave recovery). Misleading MessageBox/status-only buttons for clothing and FLUX are removed in favor of real service calls with honest gates. Interactive Windows proof and live GPU/UE5 remain external.

**CURRENT PRODUCT VERDICT:** Usable headless product path for Human Creator project fidelity is **PASS_REAL**. End-user Windows E2E with Anny/Garment/FLUX/SkinTokens remains **GATED** on install/hardware/desktop.

---

## What changed vs prior audit (capability deltas)

| Capability | Before (PR #9) | After | Label now |
|------------|----------------|-------|-----------|
| Authoritative project state | Dual CharacterSystem vs thin Anny `.3dgod` | `ActiveProjectSession` owns ProjectBundle | `WORKS_END_TO_END` (headless) / interactive UNKNOWN |
| `.3dgod` fidelity | Anny params only | Anny + MeshBytes + materials + garments + refs | `WORKS_END_TO_END` (proven Stage 27) |
| Autosave recovery | Backend only | Wired + startup prompt | `WORKS_END_TO_END` (logic) / interactive UNKNOWN |
| Form height | Uniform scale lie | Disabled for Anny; honesty banner | `FIXED` (misleading removed) |
| Clothing Jacket Fit | MessageBox only | Calls `IGarmentFitService` | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| FLUX Referenzbild | Status text only | Calls `GenerateAsync` + embeds PNG | `IMPLEMENTED_GATED_HARDWARE` |
| Image→3D | No UI | AI panel image → TripoSR | `IMPLEMENTED_GATED_EXTERNAL_RUNTIME` |
| SkinTokens Auto-Rig | No UI | Rigging panel when invocable | `IMPLEMENTED_GATED_HARDWARE` |
| AI edit | Parser backend only | Allowlisted executor on session | `WORKS_END_TO_END` for allowlisted ops |
| Export GLB | Viewport path only | Project-state materialize fallback | `WORKS_END_TO_END` |
| Export to Unreal | Disabled copy trap | Honest NotImplemented message | `NOT_IMPLEMENTED` (honest) |
| Setup Assistant | Install implied usable | Install ≠ usable copy | `WORKS_END_TO_END` (honesty) |
| Creature / remesh UI | Backend only | Still backend only (P2) | `WORKS_BACKEND_ONLY` |
| Piercings / tattoos | NotImplemented | Unchanged | `NOT_IMPLEMENTED` |

---

## Minimum HUMAN workflow (code chain)

Launch → New → Anny Human → ensure install → generate → edit params → viewport → clothing fit → material → save `.3dgod` → reopen (mesh+params+materials+garments) → undo/AI edit → export GLB.

| Step | Status |
|------|--------|
| Session New/Open/Save/Export | `PASS_REAL` (Stage 27) |
| Anny generate UI | `GATED_EXTERNAL` + `GATED_INTERACTIVE` |
| Clothing fit UI | `GATED_EXTERNAL` + invoke wired |
| Interactive WPF | `GATED_INTERACTIVE` |

---

## Remaining blockers

1. Fresh Windows install still needs Setup Assistant for Anny (honest).
2. FLUX / SkinTokens need GPU (`GATED_HARDWARE`).
3. No agent interactive WPF proof.
4. UE5 editor import still external scripts only.
5. P2 creature/freeform/remesh UI still unwired.
6. Attachments (piercings/tattoos) still NotImplemented.

---

## Classification reminder

Registered service ≠ user capability — but App panels now call the previously backend-only services listed above. Gates remain honest (no fake GLB/PNG).
