# PHASE 29 Report

- Phase: 29 – TRELLIS optional
- Status: **GATED_NOT_INSTALLED** (TRELLIS-Runtime) + **PASS** (Registry + Router)

## Was wirklich existiert (PASS)

- TRELLIS ist im Registry als optionales Backend
- Router wählt TRELLIS nicht automatisch
- `ImageTo3DTests.Probe_IsNotAvailable_AndNeverSuccess("trellis")`

## GATED_NOT_INSTALLED

- State **NotInstalled**, License nicht akzeptiert
- Probe nie Available, nie „success“
- Kein TRELLIS / TRELLIS.2 Runtime

## Gates

- Optionaler Provider bleibt explizit gated
