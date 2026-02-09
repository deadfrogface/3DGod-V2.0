"""Standalone Blender Sculpt-Script. Uses translation (move) not scale."""
import bpy
import json
import os

data_path = os.path.join("blender_embed", "sculpt_input.json")
if not os.path.exists(data_path):
    print("ERROR: sculpt_input.json not found")
    bpy.ops.wm.quit_blender()
    raise SystemExit(1)

with open(data_path, "r") as f:
    sculpt_data = json.load(f)

# Map slider values (0-100) to translation offsets. 50 = center (no change).
def to_offset(val, scale=0.1):
    return (val - 50) * scale

height = sculpt_data.get("height", 50)
hip_width = sculpt_data.get("hip_width", 50)
breast_size = sculpt_data.get("breast_size", 50)

ty = to_offset(height, 0.2)
tx = to_offset(hip_width, 0.1)
tz = to_offset(breast_size, 0.08)

for obj in bpy.data.objects:
    if obj.type == "MESH":
        obj.location.x = tx
        obj.location.y = ty
        obj.location.z = tz
        break

for obj in bpy.context.scene.objects:
    if obj.type == "MESH":
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode="SCULPT")
        break
