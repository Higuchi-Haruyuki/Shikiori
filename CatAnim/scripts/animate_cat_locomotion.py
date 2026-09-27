"""Create the three in-place locomotion loops specified in model_spec.md."""

import math

import bpy


scene = bpy.context.scene
rig = bpy.data.objects["Cat_Rig"]
assert rig.animation_data and rig.animation_data.action
assert rig.animation_data.action.name == "Cat_Idle"
for name in ("Cat_Walk", "Cat_CrouchWalk", "Cat_Dash"):
    assert name not in bpy.data.actions, name + " already exists"

scene.render.fps = 30
scene.render.fps_base = 1.0

leg_names = []
scale_names = ["Spine_02", "Spine_03"]
for region in ("Front", "Hind"):
    for side in ("L", "R"):
        upper = region + "Leg_Upper." + side
        lower = region + "Leg_Lower." + side
        foot = region + "Foot." + side
        toe = region + "Toe." + side
        leg_names.extend((upper, lower, foot, toe))
        scale_names.extend((upper, lower))

body_names = ["Spine_01", "Spine_02", "Spine_03", "Neck_01",
              "Neck_02", "Head", "Ear_01.L", "Ear_01.R"]
tail_names = ["Tail_%02d" % i for i in range(1, 9)]
rot_names = leg_names + body_names + tail_names


def reset_pose():
    for bone in rig.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)


def pose_gait(kind, t):
    reset_pose()
    phase = 2.0 * math.pi * t
    if kind == "walk":
        upper_amplitude, lower_flex = .30, .39
        phases = {"Front.L": 0.0, "Hind.R": .25,
                  "Front.R": .50, "Hind.L": .75}
        bob = -.003
        scale_front = scale_hind = 1.0
    elif kind == "crouch":
        upper_amplitude, lower_flex = .19, .26
        phases = {"Front.L": 0.0, "Hind.R": .25,
                  "Front.R": .50, "Hind.L": .75}
        bob = .071 + .002 * (1.0 - math.cos(4.0 * math.pi * t))
        scale_front, scale_hind = .82, .75
    else:  # gallop
        upper_amplitude, lower_flex = .48, .56
        phases = {"Front.L": 0.0, "Front.R": .055,
                  "Hind.L": .50, "Hind.R": .555}
        bob = -.008 - .020 * math.cos(4.0 * math.pi * t)
        scale_front = scale_hind = 1.0

    # The Hips bone's positive local Z points mostly down in world space.
    rig.pose.bones["Hips"].location[2] = bob
    for region in ("Front", "Hind"):
        for side in ("L", "R"):
            local_phase = phase + 2.0 * math.pi * phases[region + "." + side]
            stride = math.sin(local_phase)
            swing = max(0.0, -stride)
            upper_name = region + "Leg_Upper." + side
            lower_name = region + "Leg_Lower." + side
            foot_name = region + "Foot." + side
            toe_name = region + "Toe." + side
            upper = upper_amplitude * stride
            lower = -lower_flex * swing
            rig.pose.bones[upper_name].rotation_euler[0] = upper
            rig.pose.bones[lower_name].rotation_euler[0] = lower
            rig.pose.bones[foot_name].rotation_euler[0] = -.30 * upper - .25 * lower
            rig.pose.bones[toe_name].rotation_euler[0] = -.18 * lower
            scale = scale_front if region == "Front" else scale_hind
            rig.pose.bones[upper_name].scale[1] = scale
            rig.pose.bones[lower_name].scale[1] = scale

    if kind == "dash":
        rig.pose.bones["Spine_01"].rotation_euler[0] = .035 * math.cos(phase)
        rig.pose.bones["Spine_02"].rotation_euler[0] = .035 * math.cos(phase)
        rig.pose.bones["Spine_03"].rotation_euler[0] = -.025 * math.cos(phase)
        rig.pose.bones["Neck_01"].rotation_euler[0] = -.020 * math.cos(phase)
        rig.pose.bones["Head"].rotation_euler[0] = .018 * math.cos(phase)
        tail_amplitude = .020
    else:
        rig.pose.bones["Spine_01"].rotation_euler[0] = .012 * math.cos(2 * phase)
        rig.pose.bones["Spine_02"].rotation_euler[0] = .010 * math.cos(2 * phase)
        rig.pose.bones["Spine_03"].rotation_euler[0] = -.008 * math.cos(2 * phase)
        rig.pose.bones["Neck_01"].rotation_euler[0] = -.008 * math.cos(2 * phase)
        rig.pose.bones["Head"].rotation_euler[0] = .008 * math.cos(2 * phase)
        tail_amplitude = .012 if kind == "crouch" else .016

    for i, name in enumerate(tail_names, 1):
        rig.pose.bones[name].rotation_euler[0] = (
            (tail_amplitude + .003 * i) * math.cos(phase + .10 * i)
        )


def create_action(name, kind, duration):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    for frame in range(1, duration + 1):
        t = (frame - 1) / (duration - 1)
        pose_gait(kind, t)
        for bone_name in rot_names:
            rig.pose.bones[bone_name].keyframe_insert(
                data_path="rotation_euler", frame=frame, group=bone_name
            )
        for bone_name in scale_names:
            rig.pose.bones[bone_name].keyframe_insert(
                data_path="scale", frame=frame, group=bone_name
            )
        rig.pose.bones["Hips"].keyframe_insert(
            data_path="location", frame=frame, group="Hips"
        )
    for curve in action.fcurves:
        for point in curve.keyframe_points:
            point.interpolation = "BEZIER"
            point.handle_left_type = "AUTO_CLAMPED"
            point.handle_right_type = "AUTO_CLAMPED"
    return action


# Explicit resting leg and hip channels let the idle action follow a locomotion
# preview without retaining the last clip's crouch scales or limb rotations.
idle = rig.animation_data.action
reset_pose()
for frame in (1, 60):
    for bone_name in leg_names + ["Spine_01"]:
        rig.pose.bones[bone_name].keyframe_insert(
            data_path="rotation_euler", frame=frame, group=bone_name
        )
    for bone_name in scale_names[2:]:
        rig.pose.bones[bone_name].keyframe_insert(
            data_path="scale", frame=frame, group=bone_name
        )
    rig.pose.bones["Hips"].keyframe_insert(
        data_path="location", frame=frame, group="Hips"
    )

created = [
    create_action("Cat_Walk", "walk", 24),
    create_action("Cat_CrouchWalk", "crouch", 32),
    create_action("Cat_Dash", "dash", 16),
]
rig.animation_data.action = created[0]
scene.frame_start = 1
scene.frame_end = 24
scene.frame_set(1)
rig["rig_stage"] = "4 actions: idle, walk, crouch walk, dash"
print([(action.name, tuple(action.frame_range), len(action.fcurves))
       for action in [idle] + created])
