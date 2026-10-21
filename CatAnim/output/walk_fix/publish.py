import bpy
import shutil
from pathlib import Path

root=Path.cwd()
source=root/'Source/Cat/CH_Cat.blend'
backup=source.with_name('CH_Cat_before_walk_fix.blend')
assert not backup.exists(), 'Do not overwrite the original backup'
shutil.copy2(source,backup)
scene=bpy.context.scene
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source))

# Quarter-frame export samples, with the same timing in seconds.
for action in bpy.data.actions:
    for curve in action.fcurves:
        for key in curve.keyframe_points:
            key.co.x *= 4
            key.handle_left.x *= 4
            key.handle_right.x *= 4
scene.render.fps=120
scene.frame_start=4
scene.frame_end=96
scene.frame_set(4)
for obj in bpy.data.objects:obj.select_set(False)
mesh=bpy.data.objects['Cat_Body'];rig=bpy.data.objects['Cat_Rig']
mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(root/'output/walk_fix/cat_rigged_anim_walk_fixed.glb'),
    export_format='GLB',use_selection=True,export_animations=True,
    export_animation_mode='ACTIONS',export_frame_range=False,export_skins=True,
    export_influence_nb=4,export_all_influences=False)
