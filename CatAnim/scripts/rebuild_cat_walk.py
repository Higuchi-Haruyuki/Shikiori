"""Bake a planted, four-beat walk into Cat_Walk using analytic leg IK.

Call rebuild_walk() in Blender. Repairs limb weights once and replaces only
the Cat_Walk animation; saving/exporting is left to the caller.
"""

import math
import runpy
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

DUTY = .65
STRIDE = .10
LAST_FRAME = 24
# Touchdowns: left hind, left fore, right hind, right fore.
TOUCHDOWN = {'Hind.L': 0., 'Front.L': .25, 'Hind.R': .5, 'Front.R': .75}


def foot_target(region, side, t):
    phase = (t - TOUCHDOWN[region + '.' + side]) % 1.0
    center = -.20 if region == 'Front' else .19
    lift = .014 if region == 'Front' else .018
    if phase < DUTY:
        y = center - STRIDE / 2 + STRIDE * phase / DUTY
        z = 0.
    else:
        u = (phase - DUTY) / (1 - DUTY)
        # Match the stance velocity at both ends of the recovery segment.
        tangent = (1 - DUTY) / DUTY
        progress = (-2*u**3 + 3*u**2) - tangent*(2*u**3 - 3*u**2 + u)
        y = center + STRIDE / 2 - STRIDE * progress
        z = lift * 16 * u*u * (1-u)*(1-u)
    return Vector(((.085 if region == 'Front' else .105) * (1 if side == 'L' else -1),
                   y, (.055 if region == 'Front' else .065) + .001 + z)), phase


def set_segment(bone, head, tail):
    rest = bone.bone
    turn = (rest.tail_local - rest.head_local).rotation_difference(tail-head)
    rotation = turn @ rest.matrix_local.to_quaternion()
    bone.matrix = Matrix.LocRotScale(head, rotation, Vector((1, 1, 1)))
    bpy.context.view_layer.update()


def solve_leg(rig, region, side, target, shoulder_offset=None, lengths=None):
    upper = rig.pose.bones[region + 'Leg_Upper.' + side]
    lower = rig.pose.bones[region + 'Leg_Lower.' + side]
    foot = rig.pose.bones[region + 'Foot.' + side]
    toe = rig.pose.bones[region + 'Toe.' + side]
    origin = upper.head.copy()
    if shoulder_offset is not None:
        origin += shoulder_offset
    delta = target-origin
    distance = delta.length
    a, b = lengths or (upper.bone.length, lower.bone.length)
    if not abs(a-b)+1e-6 < distance < a+b-1e-6:
        raise ValueError('Unreachable foot target: %s.%s %.6f / %.6f' %
                         (region, side, distance, a+b))
    direction = delta.normalized()
    # Preserve the bend side actually built into this mesh's rest skeleton.
    rest_axis=(lower.bone.tail_local-upper.bone.head_local).normalized()
    rest_joint=lower.bone.head_local-upper.bone.head_local
    pole=(rest_joint-rest_axis*rest_joint.dot(rest_axis)).normalized()
    bend = (pole-direction*pole.dot(direction)).normalized()
    along = (a*a-b*b+distance*distance)/(2*distance)
    joint = origin + along*direction + math.sqrt(max(0, a*a-along*along))*bend
    set_segment(upper, origin, joint)
    upper.scale[1] = a / upper.bone.length
    bpy.context.view_layer.update()
    set_segment(lower, joint, target)
    lower.scale[1] = b / lower.bone.length
    bpy.context.view_layer.update()
    # Keep the paw's rest orientation in armature space. Thus the whole sole
    # travels on a horizontal plane throughout stance, including body sway.
    foot.matrix = Matrix.LocRotScale(target, foot.bone.matrix_local.to_quaternion(), Vector((1,1,1)))
    bpy.context.view_layer.update()
    # A reachable foot is insufficient: reject poses outside the usable
    # deformation range instead of forcing the mesh to satisfy contact.
    for bone in (upper,lower,foot):
        angle=bone.rotation_euler.to_quaternion().angle
        if angle>math.radians(85):
            raise ValueError('Excessive joint rotation: %s %.1f deg' % (bone.name,math.degrees(angle)))
    toe.matrix = Matrix.LocRotScale(foot.tail, toe.bone.matrix_local.to_quaternion(), Vector((1,1,1)))
    bpy.context.view_layer.update()


def rebuild_walk():
    scene = bpy.context.scene
    rig = bpy.data.objects['Cat_Rig']
    mesh = bpy.data.objects['Cat_Body']
    runpy.run_path(str(Path(__file__).resolve().with_name('fix_cat_leg_weights.py')))['apply_leg_weights'](mesh)
    # Correctly bound soles need only a constant rest-space floor offset.
    soles = {}
    for region in ('Front','Hind'):
        for side,sign in (('L',1),('R',-1)):
            ids = [v.index for v in mesh.data.vertices if v.co.z < .018
                   and sign*v.co.x > .04
                   and (v.co.y < -.16 if region == 'Front' else v.co.y > .12)]
            soles[region,side] = -min(mesh.data.vertices[i].co.z for i in ids)
    action = bpy.data.actions.get('Cat_Walk')
    if action is None:
        action = bpy.data.actions.new('Cat_Walk')
    for curve in list(action.fcurves):
        action.fcurves.remove(curve)
    action.use_fake_user = True
    rig.animation_data.action = action
    scene.render.fps = 30
    scene.render.fps_base = 1
    tracked = []
    for region in ('Front','Hind'):
        for side in ('L','R'):
            tracked += [region + part + '.' + side for part in ('Leg_Upper','Leg_Lower','Foot','Toe')]
    body = ['Spine_01','Spine_02','Spine_03','Neck_01','Neck_02','Head','Ear_01.L','Ear_01.R']
    tail = ['Tail_%02d' % i for i in range(1,9)]
    for step in range((LAST_FRAME-1)*4+1):
        frame = 1 + step/4
        scene.frame_set(int(frame), subframe=frame-int(frame))
        t = (frame-1)/(LAST_FRAME-1)
        phase = 2*math.pi*t
        for bone in rig.pose.bones:
            bone.rotation_mode='XYZ'
            bone.location=(0,0,0)
            bone.rotation_euler=(0,0,0)
            bone.scale=(1,1,1)
        hips = rig.pose.bones['Hips']
        offset = Vector((.0015*math.sin(phase), 0, -.016 + .001*math.cos(2*phase)))
        hips.location = hips.bone.matrix_local.to_3x3().inverted() @ offset
        # Subtle shoulder/hip load transfer; the IK compensates at the feet.
        rig.pose.bones['Spine_01'].rotation_euler[1] = .004*math.sin(phase)
        rig.pose.bones['Spine_03'].rotation_euler[1] = -.004*math.sin(phase)
        rig.pose.bones['Head'].rotation_euler[1] = .002*math.sin(phase)
        for i,name in enumerate(tail,1):
            rig.pose.bones[name].rotation_euler[0] = (.010+.002*i)*math.sin(phase+.12*i)
        bpy.context.view_layer.update()
        for region in ('Front','Hind'):
            for side in ('L','R'):
                target,leg_phase = foot_target(region,side,t)
                target.z += soles[region,side]
                shoulder = None
                if region == 'Front' and rig.get('front_leg_anatomy_version') == 1:
                    # Scapular glide accompanies the humerus during support
                    # and recovery, instead of leaving the shoulder fixed.
                    shoulder = Vector((0, .012*math.sin(2*math.pi*(leg_phase-.15)),
                                       .002*math.cos(2*math.pi*leg_phase)))
                solve_leg(rig,region,side,target,shoulder)
        for name in tracked+body+tail:
            bone=rig.pose.bones[name]
            bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
            if name in tracked or name in ('Spine_02','Spine_03'):
                bone.keyframe_insert(data_path='scale',frame=frame,group=name)
            if name.startswith('FrontLeg_Upper.'):
                bone.keyframe_insert(data_path='location',frame=frame,group=name)
        hips.keyframe_insert(data_path='location',frame=frame,group='Hips')
    for curve in action.fcurves:
        for key in curve.keyframe_points:
            key.interpolation='BEZIER'
            key.handle_left_type='AUTO_CLAMPED'
            key.handle_right_type='AUTO_CLAMPED'
        curve.modifiers.new('CYCLES')
    action['stance_ratio']=DUTY
    action['stride_m']=STRIDE
    action['nominal_speed_mps']=STRIDE/(DUTY*(LAST_FRAME-1)/30)
    action['gait_method']='Baked analytic IK; planted paws; lateral four-beat walk'
    rig['walk_speed_mps']=action['nominal_speed_mps']
    scene.frame_start=1
    scene.frame_end=LAST_FRAME
    scene.frame_set(1)
    return action


if __name__ == '__main__':
    rebuild_walk()
