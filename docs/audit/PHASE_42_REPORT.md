# PHASE 42 Report

- Phase: 42 – GarmentCode / PyGarment POC
- Status: **PASS** (Pattern-Import real; Warp-Drape bleibt ungebündelt)

## Was wirklich existiert

- Isolierter Worker `workers/garmentcode` (uv, Python 3.12, `pygarment` aus `uv.lock`).
- PyPI-Layout-Shim: `pygarment.garmentcode` / `pygarment.pattern` zeigen auf die flatten-Pakete `garmentcode/` + `pattern/`.
- `garment.jacket` erzeugt eine **parametrische Jacke** über vier echte `Panel`/`Edge`-Objekte:
  `jacket-front`, `jacket-back`, `jacket-sleeve-L`, `jacket-sleeve-R`.
- Auf Platte: Pattern-JSON (≥4 Panels) + trianguliertes OBJ (cm→m) + SharpGLTF-GLB.
- Längere Ärmel (`sleeveLengthCm`) vergrößern die |X|-Spanweite. Kein Fake-Mesh.
- Feature-Gate `garment.garmentcode` = **Experimental** bei uv+Lock, sonst **NotInstalled**.
- Lizenznotiz: `docs/licenses/GARMENTCODE.md`. MIT-Core. **Kein Release-Bundling** von CGAL/libigl/Warp.

## Nicht vorhanden

- Kein Warp-/Cloth-Drape (`simulated: false`).
- Kein Cairo-SVG (Component/`VisPattern` nicht importiert).
- Kein Body-Fit (PHASE 43). `clothing.fit` bleibt **NotImplemented**.

## Gates

- `dotnet build -c Release`: 0 Fehler, 0 Warnungen
- `dotnet test -c Release` (ohne `AnnyGenerate`): 187 bestanden, inkl. `GarmentCodeTests` (4 Panels, GLB, Sleeve-Span)
- Kein Fake-GLB ohne Worker. Warp-Drape nicht gebündelt.
