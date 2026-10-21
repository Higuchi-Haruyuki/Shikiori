import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

root=Path.cwd();s=bpy.context.scene
m=bpy.data.objects['Cat_Body'];r=bpy.data.objects['Cat_Rig']
r.animation_data.action=bpy.data.actions['Cat_Walk']
source={};dg=bpy.context.evaluated_depsgraph_get()
expected={'Cat_Idle':(1,60),'Cat_Walk':(1,24),'Cat_CrouchWalk':(1,32),'Cat_Dash':(1,16)}
for name,interval in expected.items():
    for p in r.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
    r.animation_data.action=bpy.data.actions[name]
    for f in range(interval[0],interval[1]+1):
        s.frame_set(f);e=m.evaluated_get(dg);data=e.to_mesh()
        source[name,f]=[m.matrix_world@v.co for v in data.vertices];e.to_mesh_clear()
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.render.fps=30
bpy.ops.import_scene.gltf(filepath=str(root/'output/front_leg_fix/cat_rigged_anim_walk_fixed.glb'))
r=bpy.data.objects['Cat_Rig'];m=bpy.data.objects['Cat_Body']
expected={'Cat_Idle':(1,60),'Cat_Walk':(1,24),'Cat_CrouchWalk':(1,32),'Cat_Dash':(1,16)}
for name,interval in expected.items():
    action=next(a for a in bpy.data.actions if a.name.startswith(name))
    assert all(abs(x-y)<1e-5 for x,y in zip(action.frame_range,interval))
r.animation_data.action=next(a for a in bpy.data.actions if a.name.startswith('Cat_Walk'))
for v in m.data.vertices:
    assert len(v.groups)<=4 and abs(sum(g.weight for g in v.groups)-1)<1e-6
    x,y,z=v.co
    if z<.16 and abs(x)>.04 and (y<-.13 or y>.10):
        region='Front' if y<0 else 'Hind';side='L' if x>0 else 'R'
        assert all(m.vertex_groups[g.group].name.startswith(region) and m.vertex_groups[g.group].name.endswith('.'+side) for g in v.groups)
dg=bpy.context.evaluated_depsgraph_get();max_error=0.;errors={name:0. for name in expected}
for (name,f),points in source.items():
    if f==1:
        for p in r.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
        r.animation_data.action=next(a for a in bpy.data.actions if a.name.startswith(name))
    s.frame_set(f);e=m.evaluated_get(dg);data=e.to_mesh();tree=KDTree(len(data.vertices))
    for v in data.vertices:tree.insert(m.matrix_world@v.co,v.index)
    tree.balance();error=max(tree.find(p)[2] for p in points);max_error=max(max_error,error);errors[name]=max(errors[name],error)
    e.to_mesh_clear()
assert max_error<.00002,max_error
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
for obj in s.objects:
    if obj.type=='MESH' and obj!=m:obj.hide_render=True
camera=bpy.data.cameras.new('QA_Camera');obj=bpy.data.objects.new('QA_Camera',camera);s.collection.objects.link(obj);s.camera=obj
camera.type='ORTHO';camera.ortho_scale=.80;target=Vector((0,-.03,.29))
obj.location=target+Vector((1,-.25,.09));obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=700;s.render.resolution_y=600;s.render.resolution_percentage=100
r.animation_data.action=next(a for a in bpy.data.actions if a.name.startswith('Cat_Walk'));s.frame_set(19)
s.render.filepath=str(root/'output/front_leg_fix/glb_frame_19.png');bpy.ops.render.render(write_still=True)
report=dict(source_glb_max_surface_error_m=max_error,all_four_clips_present=True,
            per_clip_max_surface_error_m=errors,
            animation_durations_preserved=True,limb_weight_separation_verified=True)
(root/'output/front_leg_fix/glb_verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('GLB_VERIFIED',json.dumps(report))

