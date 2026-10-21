import bpy
import json
from pathlib import Path
from mathutils import Vector

root=Path.cwd()

def protected():
    return repr((
        [(a.name,[(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in bpy.data.actions if a.name!='Cat_Walk'],
        [(tuple(v.co),[(g.group,g.weight) for g in v.groups]) for v in bpy.data.objects['Cat_Body'].data.vertices],
        [(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in bpy.data.objects['Cat_Rig'].data.bones]))

before=protected()
bpy.ops.wm.open_mainfile(filepath=str(root/'output/walk_fix/candidate.blend'))
assert protected()==before, 'Other actions, mesh, weights or rest bones changed'
s=bpy.context.scene;r=bpy.data.objects['Cat_Rig'];m=bpy.data.objects['Cat_Body'];a=bpy.data.actions['Cat_Walk']
r.animation_data.action=a
speed=a['nominal_speed_mps'];duty=a['stance_ratio'];period=23/30
offsets={'Hind.L':0.,'Front.L':.25,'Hind.R':.5,'Front.R':.75}
patches={}
tracks={}
for region in ['Front','Hind']:
    for side,sign in [('L',1),('R',-1)]:
        key=region+'.'+side
        patches[key]=[v.index for v in m.data.vertices if v.co.z<.018 and sign*v.co.x>.04 and (v.co.y<-.16 if region=='Front' else v.co.y>.12)]
        tracks[key]=[]
dg=bpy.context.evaluated_depsgraph_get()
endpoints=[]
for i in range(369):
    t=i/368;frame=1+23*t
    s.frame_set(int(frame),subframe=frame-int(frame))
    e=m.evaluated_get(dg);data=e.to_mesh()
    if i in (0,368): endpoints.append([v.co.copy() for v in data.vertices])
    for key,ids in patches.items():
        phase=(t-offsets[key])%1
        if .015<phase<duty-.015:
            center=sum((data.vertices[v].co for v in ids),Vector())/len(ids)
            # Compensating for forward travel must leave each planted sole
            # at one world location throughout a contiguous stance interval.
            tracks[key].append((t,center.y-speed*t*period,min(data.vertices[v].co.z for v in ids)))
    e.to_mesh_clear()
report={}
for key,values in tracks.items():
    segments=[[]]
    for v in values:
        if segments[-1] and v[0]-segments[-1][-1][0]>.01:segments.append([])
        segments[-1].append(v)
    slide=max(max(v[1] for v in seg)-min(v[1] for v in seg) for seg in segments)
    floor=max(abs(v[2]-.001) for v in values)
    assert slide<.0002,(key,slide)
    assert floor<.0002,(key,floor)
    report[key]={'stance_world_slide_m':slide,'stance_floor_error_m':floor}
loop=max((x-y).length for x,y in zip(*endpoints))
assert loop<1e-6,loop
report.update(loop_position_error_m=loop,other_actions_and_weights_unchanged=True,
              nominal_speed_mps=speed,stance_ratio=duty,frame_range=[1,24],fps=30)
(root/'output/walk_fix/verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VERIFIED',json.dumps(report))
