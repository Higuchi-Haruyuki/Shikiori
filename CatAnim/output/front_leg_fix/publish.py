"""Publish the reviewed candidate and retain the preceding source/export."""
import shutil
from pathlib import Path
import bpy

root = Path.cwd()
folder = root / 'output/front_leg_fix'
source = root / 'Source/Cat/CH_Cat.blend'
backup = source.with_name('CH_Cat_before_front_leg_fix.blend')
glb = folder / 'cat_rigged_anim_walk_fixed.glb'
old_glb = root / 'output/walk_shape_fix/cat_rigged_anim_walk_fixed.glb'
glb_backup = folder / 'cat_rigged_anim_before_front_leg_fix.glb'
assert glb.exists() and (folder / 'glb_verification.json').exists()
assert not backup.exists() and not glb_backup.exists(), 'Preserve existing backups'
shutil.copy2(source, backup)
shutil.copy2(old_glb, glb_backup)
bpy.ops.wm.open_mainfile(filepath=str(folder / 'candidate.blend'))
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source))
for destination in (old_glb, root / 'output/walk_fix/cat_rigged_anim_walk_fixed.glb'):
    shutil.copy2(glb, destination)
print('PUBLISHED_FRONT_LEG_FIX', source, glb)
