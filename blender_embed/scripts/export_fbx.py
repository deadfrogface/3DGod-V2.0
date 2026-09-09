"""Headless GLB → FBX export for UE5-friendly output (no Blender UI)."""
import bpy
import sys
import json
import os

SOURCE_GLB = os.environ.get("SOURCE_GLB", "")
DEST_FBX = os.environ.get("DEST_FBX", "")
PRESET_PATH = os.environ.get("PRESET_PATH", "")

args = sys.argv
output_name = "exported_character"
if "--" in args:
    idx = args.index("--")
    trailing = args[idx + 1 :]
    if len(trailing) >= 2:
        SOURCE_GLB = trailing[0]
        DEST_FBX = trailing[1]
    elif len(trailing) == 1:
        output_name = trailing[0]
        if not DEST_FBX:
            DEST_FBX = os.path.join("exports", f"{output_name}.fbx")
        if not PRESET_PATH:
            PRESET_PATH = os.path.join("presets", f"{output_name}.json")

if not DEST_FBX:
    DEST_FBX = os.path.join("exports", f"{output_name}.fbx")
if not PRESET_PATH:
    PRESET_PATH = os.path.join("presets", f"{output_name}.json")

os.makedirs(os.path.dirname(os.path.abspath(DEST_FBX)), exist_ok=True)

materials = {}
if PRESET_PATH and os.path.exists(PRESET_PATH):
    with open(PRESET_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)
        materials = data.get("materials", {})


def apply_material(obj, mat_data):
    if obj.type != "MESH":
        return
    mat = bpy.data.materials.new(name="Material_" + obj.name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and mat_data:
        hex_color = str(mat_data.get("color", "#cccccc")).lstrip("#")
        if len(hex_color) >= 6:
            r = int(hex_color[0:2], 16) / 255
            g = int(hex_color[2:4], 16) / 255
            b = int(hex_color[4:6], 16) / 255
            bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
        bsdf.inputs["Roughness"].default_value = mat_data.get("roughness", 0.5)
        bsdf.inputs["Metallic"].default_value = mat_data.get("metallic", 0.0)

        tex_path = mat_data.get("texture", "")
        if tex_path and os.path.exists(tex_path):
            tex_image = mat.node_tree.nodes.new("ShaderNodeTexImage")
            tex_image.image = bpy.data.images.load(tex_path)
            mat.node_tree.links.new(tex_image.outputs["Color"], bsdf.inputs["Base Color"])

    obj.data.materials.clear()
    obj.data.materials.append(mat)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.armatures):
        if block.users == 0:
            bpy.data.armatures.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def import_glb(path):
    abs_path = os.path.abspath(path)
    if not os.path.exists(abs_path):
        sys.stderr.write(f"GLB source missing: {abs_path}\n")
        raise SystemExit(1)
    bpy.ops.import_scene.gltf(filepath=abs_path)


def apply_preset_materials():
    bpy.ops.object.select_all(action="SELECT")
    for obj in bpy.context.selected_objects:
        name = obj.name.lower()
        if "skin" in name:
            apply_material(obj, materials.get("skin", {}))
        elif "clothes" in name or "cloth" in name:
            apply_material(obj, materials.get("clothes", {}))
        elif "piercing" in name:
            apply_material(obj, materials.get("piercings", {}))
        elif "tattoo" in name:
            apply_material(obj, materials.get("tattoos", {}))


# Bone-Mapping für UE5 / MetaHuman (best-effort rename only)
bone_rename_map = {
    "spine": "spine_01",
    "head": "head",
    "nipple.L": "nipple_l",
    "nipple.R": "nipple_r",
    "genital": "pelvis_attachment",
    "cloth_back": "coat_back",
    "piercing_nose": "nose_piercing",
}


def rename_bones_for_ue():
    for obj in bpy.data.objects:
        if obj.type == "ARMATURE":
            for bone in obj.data.bones:
                if bone.name in bone_rename_map:
                    bone.name = bone_rename_map[bone.name]


if SOURCE_GLB:
    clear_scene()
    import_glb(SOURCE_GLB)
    if materials:
        apply_preset_materials()
else:
    bpy.ops.object.select_all(action="SELECT")
    if materials:
        apply_preset_materials()

rename_bones_for_ue()

try:
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(DEST_FBX),
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        object_types={"ARMATURE", "MESH"},
        use_armature_deform_only=True,
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
    )
except Exception as e:
    sys.stderr.write(f"FBX Export error: {e}\n")
    raise SystemExit(1)

if not os.path.exists(os.path.abspath(DEST_FBX)):
    sys.stderr.write(f"FBX not written: {DEST_FBX}\n")
    raise SystemExit(1)

print(f"FBX_EXPORT_OK {os.path.abspath(DEST_FBX)}")
