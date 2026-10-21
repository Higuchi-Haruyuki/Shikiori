import bpy
from pathlib import Path
from mathutils import Vector
root=Path.cwd();s=bpy.context.scene;r=bpy.data.objects['Cat_Rig']
s.render.engine='BLENDER_WORKBENCH'
s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
s.render.resolution_x=800;s.render.resolution_y=650;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';c=s.camera;c.data.type='ORTHO';c.data.ortho_scale=.83
target=Vector((0,-.03,.27))
for view,offset in [('side',(1,0,.08)),('front',(.1,-1,.08))]:
    c.location=target+Vector(offset);c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler()
    for action,frames in [('Cat_Walk',(1,7,13,19)),('Cat_Idle',(31,)),('Cat_CrouchWalk',(9,)),('Cat_Dash',(5,))]:
        r.animation_data.action=bpy.data.actions[action]
        for p in r.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
        for frame in frames:
            s.frame_set(frame);s.render.filepath=str(root/f'output/front_leg_fix/{view}_{action}_{frame:02d}.png')
            bpy.ops.render.render(write_still=True)
