"""Standalone Blender Sculpt-Script (keine Python-Projekt-Abhängigkeiten)."""
import bpy
import json
import os

data_path = os.path.join("blender_embed", "sculpt_input.json")
if not os.path.exists(data_path):
    bpy.ops.wm.quit_blender()
    raise SystemExit(1)

with open(data_path, "r") as f:
    sculpt_data = json.load(f)

scale_factor = sculpt_data.get("height", 50) / 50
for obj in bpy.data.objects:
    if obj.type == "MESH":
        obj.scale = (scale_factor, scale_factor, scale_factor)

for obj in bpy.context.scene.objects:
    if obj.type == "MESH":
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode="SCULPT")
        break
