import bpy
import json
import runpy
import shutil
from pathlib import Path

root = Path.cwd()
out = root / 'output/face_weight_fix'
mesh = bpy.data.objects['Cat_Body']
rig = bpy.data.objects['Cat_Rig']
scene = bpy.context.scene
original_action = rig.animation_data.action
original_frame = scene.frame_current


def animation_signature():
    return repr([(a.name, [(c.data_path, c.array_index,
                 [(tuple(k.co), tuple(k.handle_left), tuple(k.handle_right), k.interpolation)
                  for k in c.keyframe_points]) for c in a.fcurves])
                 for a in bpy.data.actions])


def weights():
    return [{mesh.vertex_groups[g.group].name: g.weight for g in v.groups}
            for v in mesh.data.vertices]


face = [v.index for v in mesh.data.vertices if v.co.y < -.28 and .35 < v.co.z < .52]
dg = bpy.context.evaluated_depsgraph_get()


def sample(frame):
    scene.frame_set(frame)
    evaluated = mesh.evaluated_get(dg)
    data = evaluated.to_mesh()
    transform = (rig.pose.bones['Head'].matrix @ rig.data.bones['Head'].matrix_local.inverted()).inverted()
    result = [transform @ data.vertices[i].co for i in face]
    evaluated.to_mesh_clear()
    return result


def deformation():
    rig.animation_data.action = bpy.data.actions['Cat_Idle']
    reference = sample(1)
    return max((a-b).length for frame in range(1,61) for a,b in zip(reference,sample(frame)))


before_weights = weights()
before_anim = animation_signature()
before_deformation = deformation()
fix = runpy.run_path(str(root / 'scripts/fix_cat_face_weights.py'))['apply_face_weights']
changed = fix(mesh)
after_weights = weights()
after_deformation = deformation()
assert after_deformation < 1e-6, after_deformation
assert animation_signature() == before_anim
assert fix(mesh) == 0, 'Repair must be safe to rerun'
for v, old, new in zip(mesh.data.vertices, before_weights, after_weights):
    assert abs(sum(new.values())-1) < 1e-6
    assert 1 <= len(new) <= 4
    if v.co.z <= .30:
        assert old == new, ('Lower body changed', v.index)
for i in face:
    assert not any(n.startswith(('Ear_', 'Neck_', 'Spine_')) for n in after_weights[i])

# Confirm the ear tips still move when the left ear twitches.
scene.frame_set(40)
evaluated=mesh.evaluated_get(dg);data=evaluated.to_mesh()
base=[v.co.copy() for v in data.vertices];evaluated.to_mesh_clear()
bone=rig.pose.bones['Ear_01.L'];rotation=bone.rotation_euler.copy()
bone.rotation_euler=(0,0,0);bpy.context.view_layer.update()
evaluated=mesh.evaluated_get(dg);data=evaluated.to_mesh()
ear_motion=max((base[v.index]-v.co).length for v in data.vertices)
face_twitch=max((base[i]-data.vertices[i].co).length for i in face)
evaluated.to_mesh_clear();bone.rotation_euler=rotation
assert ear_motion > .001
assert face_twitch < 1e-7

rig.animation_data.action=original_action
scene.frame_set(original_frame)
backup=root / 'Source/Cat/CH_Cat_before_face_weight_fix.blend'
assert not backup.exists(), 'Do not overwrite original backup'
shutil.copy2(bpy.data.filepath, backup)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)

# Export all four actions from the repaired source, without changing the saved UI state.
for obj in bpy.data.objects:
    obj.select_set(False)
mesh.select_set(True);rig.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(out/'cat_rigged_anim_fixed.glb'),
    export_format='GLB',use_selection=True,export_animations=True,
    export_animation_mode='ACTIONS',export_frame_range=False,
    export_skins=True,export_influence_nb=4,export_all_influences=False)
report=dict(changed_vertices=changed, face_vertices=len(face),
    before_face_deformation_m=before_deformation, after_face_deformation_m=after_deformation,
    face_twitch_m=face_twitch, ear_tip_motion_m=ear_motion,
    animation_unchanged=animation_signature()==before_anim,
    weights_normalized=True, max_influences=4, lower_body_unchanged=True)
(out/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VERIFICATION',json.dumps(report))
