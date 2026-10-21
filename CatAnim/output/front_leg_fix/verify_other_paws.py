import bpy,json
from pathlib import Path
root=Path.cwd()
def snapshot():
    m=bpy.data.objects['Cat_Body'];r=bpy.data.objects['Cat_Rig'];s=bpy.context.scene
    edges=[]
    for edge in m.data.edges:
        a,b=edge.vertices;u=m.data.vertices[a].co;v=m.data.vertices[b].co
        if max(u.z,v.z)<.03 and max(u.y,v.y)<-.16 and min(abs(u.x),abs(v.x))>.04 and u.x*v.x>0 and (u-v).length>.0005:
            edges.append((a,b))
    result={};dg=bpy.context.evaluated_depsgraph_get()
    for name,frames in [('Cat_Idle',(1,16,31,46,60)),('Cat_CrouchWalk',(1,5,9,13,17,21,25,29,32)),('Cat_Dash',(1,5,9,13,16))]:
        for p in r.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
        r.animation_data.action=bpy.data.actions[name]
        for f in frames:
            s.frame_set(f);e=m.evaluated_get(dg);data=e.to_mesh()
            result[name,f]=[(data.vertices[a].co-data.vertices[b].co).length for a,b in edges]
            e.to_mesh_clear()
    return result
before=snapshot()
bpy.ops.wm.open_mainfile(filepath=str(root/'output/front_leg_fix/candidate.blend'))
after=snapshot();report={}
for name in ('Cat_Idle','Cat_CrouchWalk','Cat_Dash'):
    ratios=[b/a for key in before if key[0]==name for a,b in zip(before[key],after[key])]
    report[name]=[min(ratios),max(ratios)]
print('PAW_EDGE_COMPARISON',report)
(root/'output/front_leg_fix/other_paw_verification.json').write_text(json.dumps(report,indent=2))
