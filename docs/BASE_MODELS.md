# Basis-Modelle ersetzen (rigged GLB für Slider/Blender)

Die App erwartet **rigged** GLB-Modelle, damit Slider-Deformationen und die Blender-Anbindung getestet werden können. Die aktuellen `male_base.glb` / `female_base.glb` haben **kein Armature/Skin** (nur 1 Node, 1 Mesh).

## Anforderungen der App

| Kriterium | Bedeutung |
|-----------|-----------|
| **Format** | GLB (glTF 2.0 binary) |
| **Pfad** | `assets/characters/male_base.glb`, `assets/characters/female_base.glb` |
| **HasRig** | `LogicalSkins.Count > 0` **oder** `LogicalNodes.Count > 1` (Armature/Skelett) |
| **Mesh** | Mindestens 1 Mesh |
| **Blender** | Import via `bpy.ops.import_scene.gltf`; Mesh-Objekte werden mit Position belegt, danach optional SCULPT-Modus |

Ohne Rig: Slider werden deaktiviert, Blender-Sculpt kann keine echten Deformationen anwenden.

---

## Option 1: Khronos RiggedFigure (ein Modell, gender-neutral)

- **Lizenz:** CC-BY 4.0 (Cesium)
- **Rig/Skin:** Ja (glTF-Sample für Skinning)
- **Download (GLB):**  
  https://github.com/KhronosGroup/glTF-Sample-Models/raw/main/2.0/RiggedFigure/glTF-Binary/RiggedFigure.glb

**Einsatz:**  
- Datei herunterladen, als `male_base.glb` **und** als `female_base.glb` in `assets/characters/` kopieren (gleiches Modell für beide, nur zum Testen von Slider/Blender).
- Oder nur eine Variante ersetzen und die andere vorerst lassen.

---

## Option 2: Quaternius Universal Base Characters (männlich + weiblich)

- **Lizenz:** CC0
- **Inhalt:** 6 Charaktere (u. a. männlich/weiblich, verschiedene Proportionen), humanoid rigged, glTF/GLB
- **Download:**  
  https://quaternius.itch.io/universal-base-characters  
  oder  
  https://quaternius.com/packs/universalbasecharacters.html

**Schritte:**  
1. Pack herunterladen (Standard-Version reicht).  
2. Aus dem Pack die **glTF/GLB**-Dateien für einen männlichen und einen weiblichen Charakter wählen.  
3. Umbenennen in:
   - `male_base.glb` → `assets/characters/male_base.glb`
   - `female_base.glb` → `assets/characters/female_base.glb`
4. Alte `male_base.glb` / `female_base.glb` vorher sichern oder überschreiben.

---

## Option 3: Blender Studio Human Base Meshes (nach Export)

- **Lizenz:** CC0
- **Format:** Zuerst .blend (Blender), dann nach GLB exportieren
- **Download:**  
  https://download.blender.org/demo/asset-bundles/human-base-meshes/  
  (z. B. Human Base Meshes v1.4.1)

**Schritte:**  
1. .blend in Blender öffnen.  
2. Gewünschte Figur(en) auswählen (männlich/weiblich).  
3. **File → Export → glTF 2.0 (.glb)**  
   - Format: glTF Binary (.glb)  
   - Include: Selected Objects (oder Scene)  
   - Skins/Morphs inkludieren, wenn verfügbar.  
4. Exportierte GLB-Dateien als `male_base.glb` / `female_base.glb` nach `assets/characters/` legen.

---

## Nach dem Austausch prüfen

1. App starten, Form-Tab, „Männlich“ / „Weiblich“ wählen → Modell sollte laden.  
2. Kein Hinweis „Model has no armature/skin“ → Rig wird erkannt.  
3. Slider (z. B. Größe) bewegen → Vorschau/Blender reagieren.  
4. „Blender Sculpting“ starten → Blender öffnet sich, Skript lädt das GLB und wendet Offsets an.

Wenn weiterhin „no armature/skin“ erscheint: GLB enthält keine Skinning-/Node-Hierarchie; anderes Modell oder anderes Export-Setup (Skin/Joints exportieren) wählen.
