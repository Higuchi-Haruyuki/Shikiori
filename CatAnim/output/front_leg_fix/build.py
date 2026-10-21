import bpy,runpy
from pathlib import Path
root=Path.cwd()
runpy.run_path(str(root/'scripts/repair_cat_front_legs.py'))['repair_front_legs']()
bpy.ops.wm.save_as_mainfile(filepath=str(root/'output/front_leg_fix/candidate.blend'))
