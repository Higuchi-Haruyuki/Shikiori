import bpy, json, math
from pathlib import Path
from mathutils import Vector

root=Path.cwd()
def actions():
    return repr([(a.name,[(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in bpy.data.actions if a.name!='Cat_Walk'])
before_actions=actions()
before_vertices=[tuple(v.co) for v in bpy.data.objects['Cat_Body'].data.vertices]
before_face={v.index:[(g.group,g.weight) for g in v.groups] for v in bpy.data.objects['Cat_Body'].data.vertices if v.co.z>.35}
bpy.ops.wm.open_mainfile(filepath=str(root/'output/walk_shape_fix/candidate.blend'))
m=bpy.data.objects['Cat_Body'];r=bpy.data.objects['Cat_Rig'];s=bpy.context.scene
assert before_actions==actions()
assert before_vertices==[tuple(v.co) for v in m.data.vertices]
assert before_face=={v.index:[(g.group,g.weight) for g in v.groups] for v in m.data.vertices if v.co.z>.35}
for v in m.data.vertices:
    assert 1<=len(v.groups)<=4 and abs(sum(g.weight for g in v.groups)-1)<1e-6
    x,y,z=v.co
    if z<.16 and abs(x)>.04 and (y<-.13 or y>.10):
        region='Front' if y<0 else 'Hind';side='L' if x>0 else 'R'
        assert all(m.vertex_groups[g.group].name.startswith(region) and m.vertex_groups[g.group].name.endswith('.'+side) for g in v.groups)
patches={}
for region in ('Front','Hind'):
    for side,sign in (('L',1),('R',-1)):
        patches[region+'.'+side]=[v.index for v in m.data.vertices if v.co.z<.018 and sign*v.co.x>.04 and (v.co.y<-.16 if region=='Front' else v.co.y>.12)]
paw_edges=[];leg_edges=[]
for edge in m.data.edges:
    a,b=edge.vertices;u=m.data.vertices[a].co;v=m.data.vertices[b].co;length=(u-v).length
    if length<.0005:continue
    if abs(u.x)>.04 and abs(v.x)>.04 and u.x*v.x>0 and ((u.y<-.13 and v.y<-.13) or (u.y>.10 and v.y>.10)):
        if max(u.z,v.z)<.03:paw_edges.append((a,b,length))
        if .04<min(u.z,v.z) and max(u.z,v.z)<.20:leg_edges.append((a,b,length))
r.animation_data.action=bpy.data.actions['Cat_Walk'];dg=bpy.context.evaluated_depsgraph_get()
offsets={'Hind.L':0.,'Front.L':.25,'Hind.R':.5,'Front.R':.75}
speed=r['walk_speed_mps'];tracks={k:[] for k in patches}
angles={p.name:0 for p in r.pose.bones if 'Leg_' in p.name or 'Foot.' in p.name}
paw_ratio=[1.,1.];leg_ratio=[1.,1.];endpoints=[]
for step in range(369):
    t=step/368;frame=1+23*t;s.frame_set(int(frame),subframe=frame-int(frame))
    e=m.evaluated_get(dg);data=e.to_mesh()
    if step in (0,368):endpoints.append([v.co.copy() for v in data.vertices])
    for key,ids in patches.items():
        phase=(t-offsets[key])%1
        if .015<phase<.635:
            center=sum((data.vertices[i].co for i in ids),Vector())/len(ids)
            tracks[key].append((t,center.y-speed*t*23/30,min(data.vertices[i].co.z for i in ids)))
    for name in angles:
        angles[name]=max(angles[name],math.degrees(r.pose.bones[name].rotation_euler.to_quaternion().angle))
    if step%4==0:
        for edges,limits in ((paw_edges,paw_ratio),(leg_edges,leg_ratio)):
            for a,b,length in edges:
                ratio=(data.vertices[a].co-data.vertices[b].co).length/length
                limits[0]=min(limits[0],ratio);limits[1]=max(limits[1],ratio)
    e.to_mesh_clear()
report={}
for key,values in tracks.items():
    segments=[[]]
    for value in values:
        if segments[-1] and value[0]-segments[-1][-1][0]>.01:segments.append([])
        segments[-1].append(value)
    slide=max(max(v[1] for v in seg)-min(v[1] for v in seg) for seg in segments)
    floor=max(abs(v[2]-.001) for v in values)
    assert slide<.0002,(key,slide)
    assert floor<.0002,(key,floor)
    report[key]=dict(stance_slide_m=slide,stance_floor_error_m=floor)
loop=max((a-b).length for a,b in zip(*endpoints))
assert loop<1e-6
assert max(angles.values())<85
assert paw_ratio[0]>.99 and paw_ratio[1]<1.01,paw_ratio
assert leg_ratio[0]>.50 and leg_ratio[1]<1.80,leg_ratio
report.update(paw_edge_length_ratio=paw_ratio,leg_edge_length_ratio=leg_ratio,
              max_local_joint_rotation_degrees=angles,loop_error_m=loop,
              other_action_curves_unchanged=True,face_weights_unchanged=True,
              nominal_speed_mps=speed)
(root/'output/walk_shape_fix/verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VERIFIED',json.dumps(report))
