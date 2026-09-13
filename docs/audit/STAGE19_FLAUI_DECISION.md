# Stage 18 — FlaUI installed-app UI automation

**Status: IMPLEMENTED_GATED_EXTERNAL_RUNNER**

## Decision update

FlaUI is allowed **only** in a dedicated test project (`3DGodCreator.UiTests`), never in production app projects.

## Suite intent (UIA3)

- Launch installed app
- Main window appears
- Open Setup Assistant
- Inspect at least one component state
- Open Settings
- Close cleanly

## CI posture

- Default PR CI: **does not** run FlaUI (hosted agents lack stable interactive desktop)
- Baseline remains `InstalledAppSmoke` / `--smoke-test`
- Full FlaUI suite is runnable on local Windows or self-hosted interactive runners

## Forbidden

Adding FlaUI PackageReference to production `.csproj` files (asserted by honesty tests).
