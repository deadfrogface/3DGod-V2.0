# Stage 18/19 — FlaUI installed-app UI automation

**Status: IMPLEMENTED_GATED_EXTERNAL_RUNNER**

## Decision

FlaUI is allowed **only** in `3DGodCreator.UiTests` (never in production `.csproj` files).

## Suite (`InstalledAppFlaUiTests`)

- Uses `Xunit.SkippableFact` + explicit skip when `THREEDGOD_INSTALL_ROOT` is unset:
  - `Skip.If(true, "GATED_EXTERNAL_RUNNER - …")` (not a silent `return` / fake pass)
- When install root + interactive desktop are present:
  - Launch installed app
  - Assert main window
  - Open Setup Assistant (Tools menu) and assert feature list / window title
  - Open Settings tab and assert Settings content
  - Close cleanly
- Missing controls after a real launch → **FAIL** (not skip)
- Menu helpers catch only `ElementNotAvailableException` — assertion failures propagate

## CI posture

- Default PR CI: **does not** run live FlaUI (hosted agents lack interactive desktop + install root)
- Baseline remains `InstalledAppSmoke` / `--smoke-test`
- Full FlaUI suite: local Windows or self-hosted interactive runners with `THREEDGOD_INSTALL_ROOT`

## Forbidden

Adding FlaUI PackageReference to production projects (asserted by honesty tests).
