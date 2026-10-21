"""Publish the visually and numerically verified candidate; retain originals."""
import bpy
import shutil
from pathlib import Path

root=Path.cwd()
source=root/'Source/Cat/CH_Cat.blend'
backup=source.with_name('CH_Cat_before_leg_shape_fix.blend')
previous_glb=root/'output/walk_fix/cat_rigged_anim_walk_fixed.glb'
glb_backup=root/'output/walk_shape_fix/cat_rigged_anim_before_leg_shape_fix.glb'
assert not backup.exists() and not glb_backup.exists(), 'Backups must not be overwritten'
shutil.copy2(source,backup)
shutil.copy2(previous_glb,glb_backup)
scene=bpy.context.scene
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source))

# Bake quarter-frame samples without changing animation time in seconds.
# The 30 fps source has already been saved before this temporary export step.
for action in bpy.data.actions:
    for curve in action.fcurves:
        for key in curve.keyframe_points:
            key.co.x*=4
            key.handle_left.x*=4
            key.handle_right.x*=4
scene.render.fps=120
scene.frame_start=4
scene.frame_end=96
scene.frame_set(4)
for obj in bpy.data.objects:
    obj.select_set(False)
mesh=bpy.data.objects['Cat_Body'];rig=bpy.data.objects['Cat_Rig']
mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
destination=root/'output/walk_shape_fix/cat_rigged_anim_walk_fixed.glb'
bpy.ops.export_scene.gltf(filepath=str(destination),export_format='GLB',
    use_selection=True,export_animations=True,export_animation_mode='ACTIONS',
    export_frame_range=False,export_skins=True,export_influence_nb=4,
    export_all_influences=False,export_extras=True)
shutil.copy2(destination,previous_glb)
print('PUBLISHED',source,destination)
