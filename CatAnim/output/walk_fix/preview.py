import bpy
from pathlib import Path
from mathutils import Vector

root=Path.cwd();s=bpy.context.scene;r=bpy.data.objects['Cat_Rig']
r.animation_data.action=bpy.data.actions['Cat_Walk']
speed=r['walk_speed_mps']
s.render.engine='BLENDER_WORKBENCH'
s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
s.render.resolution_x=800;s.render.resolution_y=650;s.render.resolution_percentage=100
c=s.camera;c.data.type='ORTHO';c.data.ortho_scale=.83
target=Vector((0,-.03,.27));c.location=target+Vector((1,-.35,.18))
c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler()
camera_base=c.location.copy()
verts=[];faces=[]
for i in range(32):
    y=-2.4+i*.1;n=len(verts)
    verts.extend([(-2,y,-.001),(2,y,-.001),(2,y+.1,-.001),(-2,y+.1,-.001)])
    faces.append((n,n+1,n+2,n+3))
data=bpy.data.meshes.new('Preview_Ground');data.from_pydata(verts,[],faces)
floor=bpy.data.objects.new('Preview_Ground',data);s.collection.objects.link(floor)
for gray in [.22,.28]:
    mat=bpy.data.materials.new('Preview_Gray');mat.diffuse_color=(gray,gray,gray,1);data.materials.append(mat)
for i,p in enumerate(data.polygons):p.material_index=i%2
def move(scene):
    distance=speed*(scene.frame_current_final-1)/30
    r.location.y=-distance
    c.location=camera_base+Vector((0,-distance,0))
bpy.app.handlers.frame_change_pre.append(move)
s.frame_set(7);s.render.image_settings.file_format='PNG'
s.render.filepath=str(root/'output/walk_fix/preview.png');bpy.ops.render.render(write_still=True)
s.frame_start=1;s.frame_end=92;s.render.fps=30
s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264'
s.render.filepath=str(root/'output/walk_fix/walk_preview.mp4')
bpy.ops.render.render(animation=True)
