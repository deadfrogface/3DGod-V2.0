"""
Standalone Blender Sculpt-Script.
Uses POSITION/translation only - NO scale.
Loads character GLB if path provided, applies translation offsets.
"""
import bpy
import json
import os
import sys

# Working directory is app base path
base_path = os.getcwd()
data_path = os.path.join(base_path, "blender_embed", "sculpt_input.json")

if not os.path.exists(data_path):
    print("ERROR: sculpt_input.json not found", file=sys.stderr)
    print(f"Expected: {data_path}", file=sys.stderr)
    # Do NOT quit - let user see the error in Blender
    raise RuntimeError("sculpt_input.json not found")

with open(data_path, "r", encoding="utf-8") as f:
    sculpt_data = json.load(f)

# Map slider values (0-100) to translation offsets. 50 = center (no change).
# POSITION only - no scale.
def to_offset(val, scale=0.1):
    return (val - 50) * scale

height = sculpt_data.get("height", 50)
hip_width = sculpt_data.get("hip_width", 50)
breast_size = sculpt_data.get("breast_size", 50)

ty = to_offset(height, 0.2)
tx = to_offset(hip_width, 0.1)
tz = to_offset(breast_size, 0.08)

# Load character GLB if path in sculpt_data
char_path = sculpt_data.get("_character_path")
if char_path and os.path.exists(char_path):
    bpy.ops.import_scene.gltf(filepath=char_path)
    bpy.ops.object.select_all(action='SELECT')
    objs = [o for o in bpy.context.selected_objects if o.type == "MESH"]
else:
    objs = [o for o in bpy.data.objects if o.type == "MESH"]

for obj in objs:
    obj.location.x = tx
    obj.location.y = ty
    obj.location.z = tz

if objs:
    bpy.context.view_layer.objects.active = objs[0]
    try:
        bpy.ops.object.mode_set(mode="SCULPT")
    except RuntimeError:
        pass

print("Sculpt script completed successfully")
