"""Create the 30 fps, 60-frame in-place Cat_Idle action in CH_Cat.blend."""

import math

import bpy


scene = bpy.context.scene
rig = bpy.data.objects["Cat_Rig"]
mesh = bpy.data.objects["Cat_Body"]
assert mesh.parent == rig
assert "Cat_Idle" not in bpy.data.actions, "Cat_Idle already exists"

scene.render.fps = 30
scene.render.fps_base = 1.0
scene.frame_start = 1
scene.frame_end = 60

for bone in rig.pose.bones:
    bone.rotation_mode = "XYZ"
    bone.location = (0.0, 0.0, 0.0)
    bone.rotation_euler = (0.0, 0.0, 0.0)
    bone.scale = (1.0, 1.0, 1.0)

action = bpy.data.actions.new("Cat_Idle")
action.use_fake_user = True
rig.animation_data_create()
rig.animation_data.action = action

frames = list(range(1, 61, 3))
if frames[-1] != 60:
    frames.append(60)

animated = ["Spine_02", "Spine_03", "Neck_01", "Neck_02", "Head"]
animated += ["Tail_%02d" % i for i in range(1, 9)]
animated += ["Ear_01.L", "Ear_01.R"]

for frame in frames:
    t = (frame - 1) / 59.0
    breath = .5 * (1.0 - math.cos(2.0 * math.pi * t))
    sway = math.cos(2.0 * math.pi * t)
    secondary = 1.0 - math.cos(4.0 * math.pi * t)
    twitch_t = (frame - 34.0) / 12.0
    twitch = (math.sin(math.pi * twitch_t) ** 2
              if 0.0 <= twitch_t <= 1.0 else 0.0)

    spine_02 = rig.pose.bones["Spine_02"]
    spine_02.scale = (1.0 + .003 * breath, 1.0, 1.0 + .003 * breath)

    spine_03 = rig.pose.bones["Spine_03"]
    spine_03.scale = (1.0 + .004 * breath, 1.0, 1.0 + .004 * breath)

    rig.pose.bones["Neck_01"].rotation_euler = (-.008 * breath, 0.0, 0.0)
    rig.pose.bones["Neck_02"].rotation_euler = (0.0, 0.0, .010 * (1.0 - sway))
    rig.pose.bones["Head"].rotation_euler = (.012 * breath, 0.0,
                                              -.008 * (1.0 - sway))

    for i in range(1, 9):
        bone = rig.pose.bones["Tail_%02d" % i]
        amplitude = .012 + .010 * i
        bone.rotation_euler = (amplitude * sway, 0.0,
                               .003 * secondary * i / 8.0)

    rig.pose.bones["Ear_01.L"].rotation_euler = (0.0, 0.0, .025 * twitch)
    rig.pose.bones["Ear_01.R"].rotation_euler = (0.0, 0.0, -.020 * twitch)

    for name in animated:
        bone = rig.pose.bones[name]
        bone.keyframe_insert(data_path="rotation_euler", frame=frame,
                             group=name)
        if name in {"Spine_02", "Spine_03"}:
            bone.keyframe_insert(data_path="scale", frame=frame,
                                 group=name)

for curve in action.fcurves:
    for point in curve.keyframe_points:
        point.interpolation = "BEZIER"
        point.handle_left_type = "AUTO_CLAMPED"
        point.handle_right_type = "AUTO_CLAMPED"

scene.frame_set(1)
rig["rig_stage"] = "Cat_Idle 作成済み"
print({
    "action": action.name,
    "frame_range": tuple(action.frame_range),
    "fps": scene.render.fps,
    "animated_bones": animated,
    "fcurves": len(action.fcurves),
    "keyed_frames": frames,
})
