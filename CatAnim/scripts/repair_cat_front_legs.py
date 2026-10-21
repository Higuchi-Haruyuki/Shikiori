"""Rebuild the front shoulder/elbow anatomy, weights, and animation together.

Run once on the previously repaired source. This function does not save files.
The mesh rest vertices, hind legs, and face weights are intentionally preserved.
"""
import math
import runpy
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector, Matrix


def fit_paw_transform(bone, target):
    """Fit world rotation/size with local TRS while retaining exportable FULL inheritance.

    Parent nonuniform scale creates shear. Match position and polar rotation/
    size rather than attempting to key a nonrepresentable local shear matrix.
    """
    position,rotation,scale=target.decompose()
    bone.matrix=target
    bpy.context.view_layer.update()
    values=np.array([*bone.rotation_euler,*[math.log(max(1e-6,v)) for v in bone.scale]])
    def residual(parameters):
        bone.rotation_euler=parameters[:3]
        bone.scale=np.exp(parameters[3:])
        bpy.context.view_layer.update()
        _,q,s=bone.matrix.decompose()
        delta=rotation.conjugated()@q
        if delta.w<0:delta.negate()
        return np.array([2*delta.x,2*delta.y,2*delta.z,
                         *[math.log(max(1e-6,s[i])/scale[i]) for i in range(3)]])
    for iteration in range(10):
        error=residual(values)
        if max(abs(error))<2e-6:break
        jacobian=np.zeros((6,6))
        for i in range(6):
            trial=values.copy();trial[i]+=.001
            jacobian[:,i]=(residual(trial)-error)/.001
        values-=np.linalg.lstsq(jacobian,error,rcond=None)[0]
    residual(values)


def repair_front_legs():
    rig=bpy.data.objects['Cat_Rig']; mesh=bpy.data.objects['Cat_Body']
    scene=bpy.context.scene
    if rig.get('front_leg_anatomy_version') == 1:
        raise RuntimeError('Front anatomy repair already applied')
    module=runpy.run_path(str(Path(__file__).with_name('rebuild_cat_walk.py')))
    solve=module['solve_leg']
    old_rest_lengths={side:sum(rig.data.bones['FrontLeg_'+part+'.'+side].length
                              for part in ('Upper','Lower')) for side in ('L','R')}
    # Preserve the actual feet of the other three clips before editing bones.
    samples={}
    for name in ('Cat_Idle','Cat_CrouchWalk','Cat_Dash'):
        for p in rig.pose.bones:
            p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
        action=bpy.data.actions[name];rig.animation_data.action=action
        records=[]
        for step in range(round((action.frame_range[1]-1)*4)+1):
            frame=1+step/4;scene.frame_set(int(frame),subframe=frame%1)
            record={}
            for side in ('L','R'):
                upper=rig.pose.bones['FrontLeg_Upper.'+side]
                lower=rig.pose.bones['FrontLeg_Lower.'+side]
                foot=rig.pose.bones['FrontFoot.'+side]
                record[side]=(upper.head.copy(),foot.matrix.copy(),
                              rig.pose.bones['FrontToe.'+side].matrix.copy(),
                              (upper.tail-upper.head).length,(lower.tail-lower.head).length)
            records.append((frame,record))
        samples[name]=records
    rig.animation_data.action=None
    for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for side,sign in (('L',1),('R',-1)):
        upper=rig.data.edit_bones['FrontLeg_Upper.'+side]
        lower=rig.data.edit_bones['FrontLeg_Lower.'+side]
        # Shoulder sits forward of elbow; carpus sits under the forequarters.
        upper.head=(sign*.085,-.20,.31)
        upper.tail=(sign*.085,-.165,.19)
        lower.head=upper.tail
        lower.tail=(sign*.085,-.20,.055)
    bpy.ops.object.mode_set(mode='OBJECT')
    rig['front_leg_anatomy_version']=1
    # Reallocate only existing front-chain weight. Body/face and other limbs
    # retain their exact memberships. The bend band follows the new elbow.
    changed=runpy.run_path(str(Path(__file__).with_name('fix_cat_front_weights.py')))['apply_front_weights'](mesh)
    # Retarget the other clips, keeping their original feet and body motion.
    for name,records in samples.items():
        action=bpy.data.actions[name];rig.animation_data.action=action
        for curve in list(action.fcurves):
            if any(curve.data_path.startswith('pose.bones["Front'+part+'.')
                   for part in ('Leg_Upper','Leg_Lower','Foot','Toe')):
                action.fcurves.remove(curve)
        for frame,record in records:
            scene.frame_set(int(frame),subframe=frame%1)
            for side,(old_origin,foot_matrix,toe_matrix,old_a,old_b) in record.items():
                for part in ('Leg_Upper','Leg_Lower','Foot','Toe'):
                    rig.pose.bones['Front'+part+'.'+side].matrix_basis=Matrix.Identity(4)
                bpy.context.view_layer.update()
                upper=rig.pose.bones['FrontLeg_Upper.'+side]
                # Retain each clip's scapular parent animation; the new rest
                # placement is evaluated under that parent transform.
                scale=(old_a+old_b)/old_rest_lengths[side]
                lengths=(upper.bone.length*scale,
                         rig.pose.bones['FrontLeg_Lower.'+side].bone.length*scale)
                delta=foot_matrix.translation-upper.head
                # A forward scapular rest needs slight load-dependent glide
                # for the longest existing dash poses; avoid hyperextension.
                glide=delta.normalized()*max(0.,delta.length-.985*sum(lengths))
                solve(rig,'Front',side,foot_matrix.translation,
                      shoulder_offset=glide,lengths=lengths)
                foot=rig.pose.bones['FrontFoot.'+side]
                fit_paw_transform(foot,foot_matrix)
                fit_paw_transform(rig.pose.bones['FrontToe.'+side],toe_matrix)
                for part in ('Leg_Upper','Leg_Lower','Foot','Toe'):
                    bone=rig.pose.bones['Front'+part+'.'+side]
                    for channel in ('location','rotation_euler','scale'):
                        bone.keyframe_insert(data_path=channel,frame=frame,group=bone.name)
        for curve in action.fcurves:
            if 'Front' in curve.data_path:
                for key in curve.keyframe_points:
                    key.interpolation='BEZIER';key.handle_left_type='AUTO_CLAMPED';key.handle_right_type='AUTO_CLAMPED'
    module['rebuild_walk']()
    print('FRONT_REPAIRED',changed)


if __name__=='__main__':
    repair_front_legs()
