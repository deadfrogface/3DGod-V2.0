# V2 Feature Audit (PHASE 00)

Stand: 08.09.2026  
Repository: https://github.com/deadfrogface/3DGod-V2.0  
Baseline-Commit: `97ffa2d` (`docs: BASE_MODELS.md - rigged GLB sources, README link`)  
Tag: `pre-v3-rearchitecture`  
Branch: `v3-rearchitecture`

Regel: Ein sichtbarer Button oder ein Status-Text ist **kein** Beweis für eine echte Funktion.  
Bewertung nur nach tatsächlichem Codepfad und Output.

## 1. Solution-Ist-Zustand

| Item | Ist |
|------|-----|
| Solution | `3DGodCreator.sln` – App + Core (PHASE 00 fügt Tests hinzu) |
| App | `3DGodCreator.App`, `net8.0-windows`, WPF, `WinExe` |
| Core | `3DGodCreator.Core`, `net8.0` |
| UI | WPF Tabs, kein AvalonDock, kein MaterialDesign, kein CommunityToolkit.Mvvm, kein DI |
| Viewport | HelixToolkit.Wpf **2.25.0** (WPF-Software/legacy, **nicht** SharpDX) |
| GLB | SharpGLTF.Toolkit **1.0.6** |
| Tests vor PHASE 00 | keine |
| Python | nicht in der App; nur Blender-Embed-Skripte |
| AI-Worker | nicht vorhanden |

### Dependency-Richtung (Verstoß gegen Zielarchitektur)

- `ThreeDGodCreator.Core` referenziert **direkt** `BlenderService`.
- `CharacterSystem` (Core) ruft Blender für Sculpt, Auto-Rig und FBX auf.
- App kennt Blender über Settings und Fehlermeldungen.

## 2. README vs. Code

README behauptet „Funktionen (vollständige Migration)“ für Form, Sculpt, NSFW, Kleidung, Physik, Material, Presets, Rigging, Export, Einstellungen, KI.

Das ist **nicht wahr**. KI ist intern als Stub markiert. Mehrere Tabs schreiben nur State oder starten Blender-GUI. Siehe Katalog unten.

## 3. Assets – was wirklich auf der Platte liegt

Erwartet laut README / `docs/BASE_MODELS.md` / `ProjectReadinessService.CheckAssets`:

- `assets/characters/male_base.glb` (~1.73 MB) – **vorhanden**, SharpGLTF lädt Meshes
- `assets/characters/female_base.glb` (~1.82 MB) – **vorhanden**, SharpGLTF lädt Meshes
- Beide GLBs haben **`LogicalSkins.Count == 0`** (kein Skin). `docs/BASE_MODELS.md` bestätigt das. Viewport setzt `IsCurrentModelRigged = HasRig && HasSkin` → Slider werden deaktiviert. Höhe bleibt trotzdem uniforme Scale, wenn ein Mesh geladen ist.

**Keine gültigen Preview-/Overlay-Bilder:**

- `assets/view_preview/*.png` und `assets/view_overlay/**/*_demo_asset.png` sind **9-Byte-Platzhalter** mit dem ASCII-Inhalt `.gitinore` (Tippfehler), **kein** PNG-Header `89 50 4E 47`. Anatomie-Fallback und Clothing-Overlays sind damit **unecht**.

Weitere echte Datei:

- `assets/body_parameters.json` (Slider-Katalog-Daten)

## 4. Button- und Command-Katalog

### 4.1 Form (`FormPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Button „Männlich“ | `BtnMale_Click` → `SetGender("male")` + `LoadBaseModel("male")` | `ConfigService` + `IViewport.LoadPreview` | Config-Gender wird gespeichert. `male_base.glb` existiert und lädt als Mesh. Skin fehlt → Warnung, Slider disabled. | **Teilweise echt**: Datei + Mesh-Load. Kein Rig/Skin. |
| Button „Weiblich“ | analog `female` | analog | analog | analog |
| Slider aus `body_parameters.json` | `UpdateSculptValue` | Viewport `ApplySculptTransform` + debounced `BlenderService.SendSculptData` | **Höhe:** uniforme `ScaleX/Y/Z` (0.6–1.4). Andere Keys: JSON nach `blender_embed/sculpt_input.json`, Viewport ändert Mesh-Topologie **nicht**. Slider werden disabled wenn `IsCurrentModelRigged == false`. | **Height = Fake-Morph** (Uniform Scale). Andere Slider = State + JSON, kein anatomisches Morphing. |

UI-Text „Slider: Verschieben des Modells in 3D“ widerspricht `ApplySculptTransform` (Scale) und dem Blender-Skript (Translation).

### 4.2 Sculpt (`SculptPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Checkbox X-Symmetrie | setzt `SculptData["symmetry"]` 0/1 | nur Dictionary | kein Mesh-Effekt in der App | **State only** |
| „Starte Sculpting“ | `CharacterSystem.Sculpt` → `LaunchSculpt` | `BlenderService.LaunchSculpt` | Startet **sichtbares** Blender-Fenster (`keepAlive: true`, kein `--background`). Skript `apply_sculpt_standalone.py` verschiebt Mesh-Location, geht in SCULPT-Mode. Status wird **sofort** „aktiv“ gesetzt, unabhängig vom Prozess. | **Echt: Prozessstart**, wenn Blender + Skript da sind. **Kein** in-app Sculpt. Verstößt gegen späteres „kein Blender-Fenster“. Status kann voreilig „aktiv“ sein. |

`LaunchSculpt` Fallback-Pfad `blender_embed/scripts/sculpt_apply.py` existiert **nicht**.

### 4.3 NSFW (`NsfwPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Anatomie-Checkboxen Haut/Fett/Muskeln/Knochen/Organe | `OnAnatomyChanged` → `RefreshLayers` | `UpdatePreviewFromAnatomy` | Wechselt Bildpfad nur wenn **kein** 3D-Modell gezeigt wird. Die PNG-Dateien sind **keine** echten PNGs (9-Byte `.gitinore`). | **Unecht** als Anatomie-View. State wird geschrieben. |
| NSFW-Layer Brüste/Genitalien/Körperbehaarung | `OnLayerChanged` | gleicher Pfad | PNGs decken diese Layer **nicht** separat ab | **State only** für 3D. Bild-Fallback ändert sich nicht sinnvoll. |

### 4.4 Kleidung (`ClothingPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| „Lade Kleidung“ | `AddAsset("clothes")` | `CharacterSystem.AddAsset` | hängt `"clothes_demo_asset"` an eine `List<string>` | **STUB / Demo-String**. Kein File-Dialog, kein Mesh, kein Fitting, kein Skinning. Overlay-PNG wird nicht geladen. |
| „Lade Piercings“ | `AddAsset("piercings")` | analog | `"piercings_demo_asset"` | **STUB** |
| „Lade Tattoos“ | `AddAsset("tattoos")` | analog | `"tattoos_demo_asset"` | **STUB** |

### 4.5 Physik (`PhysicsPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Brustphysik / Stoff / Piercing-Schwingung | `OnChanged` schreibt `PhysicsFlags` bools | keines | keine Simulation, kein BEPU, kein Blender-Physics | **FAKE** – nur Booleans, die ins Preset serialisiert werden. |

### 4.6 Material (`MaterialEditorPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Combo skin/clothes/piercings/tattoos | `CmbMaterial_SelectionChanged` leer | – | Auswahl nur lokal | UI-State |
| „Farbe wählen“ | `ColorPickerDialog` → `SetMaterialColor` | Dictionary `Materials` | Hex in Memory; Viewport-Mesh bleibt hellgrau (`GlbLoader` hardcodiert `Colors.LightGray`) | **State echt**, **Viewport-PBR unecht** |
| „Auf Preview anwenden“ | `RefreshLayers` | Anatomy-Preview / Scale | ändert GLB-Material nicht | **kein PBR-Editor** |

Roughness/Metallic existieren in `MaterialData` und Preset, **keine UI**.

### 4.7 Presets (`PresetBrowserPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Liste | `RefreshList` liest `presets/*.json` | Dateisystem | echte JSON-Dateien | **Echt** (Dateiliste) |
| Auswahl | `LoadPreset` | `PresetService.Load` | lädt State; Viewport `UpdateView` | **Echt für JSON-State**. Mesh-Reload nur wenn GLB-Pfad existiert. |
| „Liste aktualisieren“ | `RefreshList` | – | ja | **Echt** |
| Screenshot | `RenderTargetBitmap` des Fensters als `{name}.jpg` | Dateisystem | JPEG der UI, kein Viewport-only Render | **Echt** (Fenster-Screenshot) |

### 4.8 Rigging (`RiggingPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| „Auto-Rig erstellen“ | `CreateAutoRig` → `BlenderService.LaunchAutoRig` | **`LaunchAutoRig() => LaunchSculpt()`** | öffnet dasselbe Sculpt-GUI wie Sculpt-Tab | **FAKE Auto-Rig**. Kein Skinning, kein Skeleton-Output. |
| „Metahuman-kompatibles Rig exportieren“ | `ExportFbx("metahuman_rigged")` | Preset-Save + `export_fbx.py` | Dateiname enthält „metahuman“. Skript macht kleines Bone-Rename (`spine`→`spine_01` etc.), **kein** MetaHuman-/UE-Mannequin-Workflow | **Pseudo-MetaHuman**. |

### 4.9 Export (`ExportPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| „Preset speichern“ | `SavePreset` | `PresetService` | JSON unter `presets/{name}.json` | **Echt** |
| „FBX exportieren“ | `SavePreset` + `ExportFbx` | Blender `--background --python export_fbx.py` | Wenn Blender da: headless Export der **aktuellen Blender-Szene** (Default Cube, falls nichts geladen), nicht zwingend des Viewport-GLB. UI loggt **sofort** `"FBX-Export abgeschlossen"` **ohne** auf Prozess/Datei zu warten. | **Teilweise echt** (Skript existiert). **Voreiliger SUCCESS**. Keine Validierung der FBX. |
| „Ordner wählen“ | OpenFileDialog Workaround | – | setzt Zielpfad | **Echt** (Pfadwahl) |
| „Exportiere nach Unreal“ | `File.Copy` `exports/{name}.fbx` → Ordner | Dateisystem | Kopie, kein UE-Projekt, kein Skeleton-Mapping, kein Preflight | **Kein UE5-Export**. Copy-only. |

`export_fbx.py` exportiert `use_selection=False` der ganzen Szene.

### 4.10 Einstellungen (`SettingsPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Blender-Pfad + Browse | `SaveConfig` | `ConfigService` | `config.json` | **Echt** |
| Theme Dark/Light/Cyberpunk | `SaveConfig`; `MainWindow.ApplyTheme` nur beim Start | Window-Background | Theme-Wechsel in Combo speichert, **MainWindow-Hintergrund wird nicht live neu gesetzt** (nur Konstruktor) | **Teilweise** |
| NSFW-Modus | Config + `CharacterSystem.NsfwEnabled` | – | Flag; NSFW-Tab bleibt sichtbar | State |
| Controller-Unterstützung | Config `ControllerEnabled` | **keine Controller-Implementierung** | nur Boolean | **STUB** |
| „Blender testen“ | `VerifyCanLaunch` (`blender --version`) | `ProjectReadinessService` | echter Prozess `--version` | **Echt** (Launch-Check, nicht Sculpt) |
| „System-Check“ | `DiagnosticsService.RunSystemCheck` | Readiness + optional HTTP `localhost:5000` FauxPilot | Text-Report; FauxPilot ist optional/legacy | **Echt als Diagnose**, nicht als AI |

### 4.11 KI (`AiPanel`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| Prompt-Textbox | ungenutzt außer Anzeige | – | – | tot |
| „Bild laden“ | OpenFileDialog speichert Pfad | – | Dateiname in Label | **Pfadwahl echt**, keine 3D-Pipeline |
| „Erzeuge vollständige Person“ | setzt StatusLabel | **kein Service** | Text: „ONNX-Service nicht implementiert“ | **PLACEHOLDER** |
| „Erzeuge Asset (Kleidung)“ | StatusLabel | **kein Service** | analog | **PLACEHOLDER** |

Kein TripoSR, kein Image-to-3D, kein LLM.

### 4.12 Viewport (`MainWindow`)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| HelixViewport3D | `CreateViewport3D` | HelixToolkit.Wpf | Rotate (Rechtsklick), Pan (Shift+Rechtsklick), Zoom (Mausrad) | **Echt**, wenn ein Mesh geladen ist |
| GLB-Load | `GlbLoader.Load` | SharpGLTF | Mesh-Positionen/Indices/Normals, Material immer LightGray, 180° X-Rotation | **Echt für Mesh-Anzeige**, wenn Datei existiert. Kein PBR, kein Skin-Deform im Viewport. |
| OBJ/3DS/STL | Helix `ModelImporter` | – | möglich | **Echt** (Importer), keine Testassets im Repo |
| PNG-Fallback | `LoadPreviewImage` | – | Anatomie-Bilder | **Echt** |
| Focus/Reset/Selection/Gizmo/Isolate | – | – | nicht implementiert | fehlt |

### 4.13 Debug-Konsole (F12)

| UI | Handler | Service | Real output? | Bewertung |
|----|---------|---------|--------------|-----------|
| F12 Toggle | `Window_Loaded` CommandBinding | – | Panel sichtbar | **Echt** |
| Filter/Suche/Clear | lokal | – | Filter auf Logzeilen | **Echt** |
| Froggy fragen | `FroggyService.AnalyzeLog` / `AnswerQuestion` | Keyword-Matcher | heuristische Texte, kein Stack/Line | **Echt als Keyword-Hilfe**, kein Smart Diagnostics |
| System-Check | wie Settings | – | Report | **Echt** |

### 4.14 App-Lifecycle

| Item | Bewertung |
|------|-----------|
| `App.OnStartup` Dispatcher + AppDomain exception logging | **Echt** (schreibt Log). `DispatcherUnhandledException` setzt `e.Handled = true` (Exception wird geschluckt nach Log). |
| `AppLogger` rolling file `error_log.txt` | **Echt** |
| `ProjectReadinessService` | **Echt** als Checks; `CheckNuGetPackages` versucht `Assembly.Load("HelixToolkit.Wpf")` **aus Core**, das Core nicht referenziert – Check ist im App-Prozess sinnvoll, in Core-Tests falsch-negativ. `CheckDotNet` akzeptiert auch 9.x/andere als Success mit Hinweis net8.0. |
| Undo/Redo | **nicht vorhanden** |
| Projektformat `.3dgod` | **nicht vorhanden** |
| Autosave/Recovery | **nicht vorhanden** |

## 5. Bekannte Probleme (explizit)

1. **AI placeholder** – `AiPanel` generiert nichts; Status erklärt Non-Implementation. FauxPilot-Ping in Diagnostics ist Legacy, kein Backend.
2. **AutoRig → Sculpt** – `BlenderService.LaunchAutoRig() => LaunchSculpt()`.
3. **Physics booleans** – keine Simulation.
4. **Pseudo MetaHuman** – FBX-Dateiname + triviale Bone-Renames, kein MetaHuman-Pipeline.
5. **Height = uniforme Skalierung** – `ScaleTransform3D` identisch auf X/Y/Z. Kein anatomisches Morphing.
6. **Clothing ohne Fitting/Skinning** – Demo-Strings.
7. **Kein vollständiger PBR-Editor** – Viewport-Material hardcodiert.
8. **Blender zu zentral** – Core hängt an Blender; Sculpt öffnet GUI.
9. **Projekt-/Undo-/Recovery** fehlen.
10. **Voreilige Success-Meldungen** – Sculpt-Status und FBX-Export-Log ohne Wait/Verify.
11. **Preview-/Overlay-PNGs sind Platzhalter** – 9 Byte `.gitinore`, kein Bild.
12. **HelixToolkit.Wpf 2.25.0** – NU1701 (netfx-Paket auf net8.0-windows).
13. **Controller-Checkbox** ohne Implementierung.
14. **Base GLBs ohne Skin** – Mesh sichtbar, Deformations-Slider laut Validator aus.

## 6. Was tatsächlich als Baseline gilt (nicht löschen)

Echte, erhaltenswerte Teile:

- WPF-App startet, Tab-Shell, F12-Konsole
- Helix-Viewport-Steuerung (Rotate/Pan/Zoom)
- SharpGLTF-Load der mitgelieferten `male_base.glb` / `female_base.glb` (Meshes; **kein Skin**)
- Config JSON save/load
- Preset JSON save/load
- ColorPicker → Material-Dictionary
- Blender-Pfad Config + `--version`-Check + headless `blender_runtime_test.py`
- Sculpt-Input-JSON schreiben
- `export_fbx.py` als vorhandenes Fallback-Skript (Qualität separat)
- Readiness/Diagnostics/AppLogger
- `body_parameters.json` als Slider-Katalog-Daten

Nicht als echte Preview werten: `view_preview` / `view_overlay` PNG-Platzhalter.

## 7. Smoke-Tests (PHASE 00)

Projekt: `3DGodCreator.Core.Tests`

- Config roundtrip
- Preset roundtrip
- Froggy Keyword-Analyse
- `body_parameters.json` parse
- Preview/Overlay-Dateien existieren, sind aber **keine** validen PNGs (Platzhalter-Lock)
- `male_base.glb` / `female_base.glb` existieren, laden, haben Meshes, `LogicalSkins == 0`
- SharpGLTF Toolkit schreibt/liest ein minimales GLB
- CharacterSystem In-Memory-Init + nachweislicher Demo-Asset-String

Nicht getestet (kein Fake-PASS): UI-Start, Blender-GUI, FBX-Dateiinhalt, Viewport-Picking.

## 8. PHASE-00-Grenzen

Dieses Audit **ändert keine Produktfeatures**. Tests und Dokumentation nur.
