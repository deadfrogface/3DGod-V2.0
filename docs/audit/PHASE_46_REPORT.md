# PHASE 46 Report

- Phase: 46 – Bepu Accessory Physics
- Status: **PASS**

## Was wirklich existiert

- NuGet `BepuPhysics` 2.5.0-beta.29 (Apache-2.0), Projekt `ThreeDGod.Physics`.
- `AccessoryPhysicsWorld`: hängende 8-Kugel-Kette (kinematischer Anker + `BallSocket`) und 4-Perlen-Ohrring gegen statische Kopfkugel (r=0.12 bei (0, 1.55, 0)).
- Gravity + linear/angular Damping. `Step` / `Reset` / `Capture`.
- Tests: Kette bewegt sich unter Schwerkraft, kommt zur Ruhe, `Reset` stellt Posen wieder her. Ohrring-Perlen dringen nicht in den Kopf-Collider ein.
- Feature `physics.simulate` = **Experimental**. DI: `IAccessoryPhysicsService`.
- WPF: Cloth/Brust/Piercing-Checkboxen bleiben **disabled**. Kein Fake-Viewport, keine Cloth-Behauptung.

## Nicht vorhanden

- Keine Cloth-Simulation, kein Softbody, kein Ragdoll, kein Schreiben ins Authoring-Mesh, keine Viewport-Physik.

## Gates

- `dotnet build -c Release`: 0 Fehler, 0 Warnungen
- `dotnet test -c Release` (ohne `AnnyGenerate`): 198 bestanden, inkl. `AccessoryPhysicsTests`
