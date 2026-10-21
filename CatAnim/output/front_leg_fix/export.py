import bpy
from pathlib import Path
root=Path.cwd();scene=bpy.context.scene
for action in bpy.data.actions:
    for curve in action.fcurves:
        for key in curve.keyframe_points:
            key.co.x*=4;key.handle_left.x*=4;key.handle_right.x*=4
scene.render.fps=120;scene.frame_start=4;scene.frame_end=96;scene.frame_set(4)
for obj in bpy.data.objects:obj.select_set(False)
mesh=bpy.data.objects['Cat_Body'];rig=bpy.data.objects['Cat_Rig']
mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(root/'output/front_leg_fix/cat_rigged_anim_walk_fixed.glb'),
    export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',
    export_frame_range=False,export_skins=True,export_influence_nb=4,
    export_all_influences=False,export_extras=True)
