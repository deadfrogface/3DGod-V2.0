# PHASE 21 Report

- Phase: 21 – Live Parametric Human
- Status: **PASS** (State, Undo, Persistenz) + **GATED_NOT_INSTALLED** (Live-Catalog / Vertex-Tests)

## Was wirklich existiert (PASS)

- Persistente Worker-Session (`WorkerSession`) + gecachtes `anny.Anny(...)` für Debounce-Preview (wenn Runtime da).
- Inspector-Tab mit Slidern; 800 ms Debounce; Undo/Redo über `CommandStack`/`PropertyChangeCommand`.
- `.3dgod` speichert `ParametricHumanState` (Phenotype, Local, Face) und lädt ihn zurück.
- `ParameterChange_UndoRedo_RestoresValues` und `ProjectSaveReload_ReproducesAnnyState` – **ohne** Runtime.

## GATED_NOT_INSTALLED (Live-Runtime)

- `Catalog_HasAtLeastTenLiveParameters` und `TenParameters_ChangeVertices_AndAreNotUniformScale` nutzen `TestGate.NotInstalled`, wenn Anny uv/Runtime fehlt.
- Live-Catalog aus Worker ≠ statische Preset-JSON.

## Gates

- `dotnet test -c Release`: AnnyLiveTests mit ehrlichem Soft-Skip

## Bewusst nicht fertig

- `FeatureIds.HeightMorph` bleibt **NotImplemented** (WPF-Scale ist kein Morph).
