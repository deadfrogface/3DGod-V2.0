# Stage 19 — FlaUI decision

**Decision: DO NOT add FlaUI to CI. Keep InstalledAppSmoke as the installer baseline. Mark full UI automation GATED_EXTERNAL_RUNNER.**

## Why

1. Hosted GitHub `windows-latest` agents are not a reliable interactive desktop for FlaUI.
2. Flaky UI automation would destabilize the Release Gate that Stages 4–11 just made green.
3. `--smoke-test` / `InstalledAppSmoke` already covers clean-install process start + DI + config without a desktop session.

## Revisit when

A dedicated external Windows runner with an interactive session is provisioned and FlaUI scenarios are quarantined from the default PR gate.
