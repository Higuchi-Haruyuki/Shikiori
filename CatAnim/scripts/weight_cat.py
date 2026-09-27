"""Assign spatial skin weights to the staged Cat_Rig in CH_Cat.blend.

The imported mesh is split into many small islands, so bone heat weights are
unreliable. This script uses the measured body, limb, ear, and tail regions.
Run inside Blender with: exec(open(filepath, encoding='utf-8').read()).
"""

import math
from collections import Counter

import bpy
from mathutils import Vector


mesh = bpy.data.objects["Cat_Body"]
rig = bpy.data.objects["Cat_Rig"]
assert mesh.parent == rig
assert any(mod.type == "ARMATURE" and mod.object == rig for mod in mesh.modifiers)


def clamp(value):
    return max(0.0, min(1.0, value))


def smoothstep(start, end, value):
    t = clamp((value - start) / (end - start))
    return t * t * (3.0 - 2.0 * t)


def chain_weights(value, centers):
    """Linear weights between neighboring bone influence centers."""
    ordered = sorted(centers, key=lambda item: item[1])
    if value <= ordered[0][1]:
        return {ordered[0][0]: 1.0}
    if value >= ordered[-1][1]:
        return {ordered[-1][0]: 1.0}
    for (left_name, left_at), (right_name, right_at) in zip(ordered, ordered[1:]):
        if left_at <= value <= right_at:
            t = (value - left_at) / (right_at - left_at)
            return {left_name: 1.0 - t, right_name: t}
    raise RuntimeError("Bone chain lookup failed")


def segment_projection(point, start, end):
    delta = end - start
    t = clamp((point - start).dot(delta) / delta.length_squared)
    return t, (point - (start + t * delta)).length


tail_points = [Vector(v) for v in [
    (0, .18, .28), (.03, .22, .31), (.07, .26, .35),
    (.11, .29, .39), (.15, .30, .43), (.19, .30, .46),
    (.23, .30, .47), (.26, .30, .45), (.27, .31, .42),
]]


def tail_proximity(point):
    best_distance = float("inf")
    best_position = 0.0
    for i, (start, end) in enumerate(zip(tail_points, tail_points[1:])):
        t, distance = segment_projection(point, start, end)
        if distance < best_distance:
            best_distance = distance
            best_position = i + t
    return best_position, best_distance


trunk = [
    ("Hips", .15), ("Spine_01", .06), ("Spine_02", -.03),
    ("Spine_03", -.13), ("Neck_01", -.21),
    ("Neck_02", -.26), ("Head", -.32),
]

for group in list(mesh.vertex_groups):
    mesh.vertex_groups.remove(group)
groups = {bone.name: mesh.vertex_groups.new(name=bone.name)
          for bone in rig.data.bones if bone.use_deform}
used = Counter()
influence_histogram = Counter()
minimum_sum = 1.0
maximum_sum = 0.0

for vertex in mesh.data.vertices:
    point = vertex.co
    x, y, z = point
    scores = {}

    def add(name, amount):
        if amount > 0.0:
            scores[name] = scores.get(name, 0.0) + amount

    # Separate each limb by side and by its longitudinal position.
    leg_masks = []
    for side, sign in (("L", 1.0), ("R", -1.0)):
        for region, center_x, center_y, upper_z, lower_z, foot_z in (
            ("Front", sign * .085, -.20, .255, .125, .045),
            ("Hind", sign * .105, .19, .225, .12, .045),
        ):
            lateral = math.exp(-.5 * ((x - center_x) / .062) ** 2)
            longitudinal = math.exp(-.5 * ((y - center_y) / .078) ** 2)
            vertical = 1.0 - smoothstep(.23 if region == "Front" else .20,
                                        .34 if region == "Front" else .30, z)
            mask = clamp(lateral * longitudinal * vertical)
            leg_masks.append(mask)
            if mask < .005:
                continue
            prefix = "FrontLeg" if region == "Front" else "HindLeg"
            names = [
                (prefix + "_Upper." + side, upper_z),
                (prefix + "_Lower." + side, lower_z),
                (("FrontFoot" if region == "Front" else "HindFoot") + "." + side,
                 foot_z),
            ]
            local = chain_weights(z, names)
            if z < .065:
                toe = (("FrontToe" if region == "Front" else "HindToe")
                       + "." + side)
                toe_amount = (smoothstep(-.22, -.26, y) if region == "Front"
                              else smoothstep(.18, .14, y))
                toe_amount *= 1.0 - smoothstep(.035, .075, z)
                local = {name: weight * (1.0 - toe_amount)
                         for name, weight in local.items()}
                local[toe] = toe_amount
            for name, weight in local.items():
                add(name, mask * weight)

    tail_at, tail_distance = tail_proximity(point)
    tail_mask = (1.0 - smoothstep(.065, .105, tail_distance))
    tail_mask *= smoothstep(.17, .235, y)
    tail_mask *= smoothstep(.01, .06, x)
    tail_mask = clamp(tail_mask)
    if tail_mask > .005:
        local = chain_weights(tail_at, [
            ("Tail_%02d" % i, i - .5) for i in range(1, 9)
        ])
        for name, weight in local.items():
            add(name, tail_mask * weight)

    ear_masks = []
    eye_masks = []
    for side, sign in (("L", 1.0), ("R", -1.0)):
        ear_distance = math.sqrt(((x - sign * .082) / .05) ** 2
                                 + ((y + .255) / .065) ** 2)
        ear_mask = (1.0 - smoothstep(.7, 1.5, ear_distance))
        ear_mask *= smoothstep(.485, .535, z)
        ear_mask = clamp(ear_mask)
        ear_masks.append(ear_mask)
        if ear_mask > .005:
            local = chain_weights(z, [
                ("Ear_01." + side, .525), ("Ear_02." + side, .57)
            ])
            for name, weight in local.items():
                add(name, ear_mask * weight)

        eye_distance = math.sqrt(((x - sign * .042) / .032) ** 2
                                 + ((y + .33) / .04) ** 2
                                 + ((z - .46) / .036) ** 2)
        eye_mask = .7 * (1.0 - smoothstep(.45, 1.3, eye_distance))
        eye_masks.append(eye_mask)
        add("Eye." + side, eye_mask)

    jaw_distance = math.sqrt((x / .07) ** 2
                             + ((y + .355) / .045) ** 2
                             + ((z - .40) / .045) ** 2)
    jaw_mask = .75 * (1.0 - smoothstep(.45, 1.3, jaw_distance))
    add("Jaw", jaw_mask)

    secondary_mask = max([tail_mask, *ear_masks, *eye_masks, jaw_mask])
    leg_mask = clamp(sum(leg_masks))
    trunk_mask = (1.0 - leg_mask) * (1.0 - secondary_mask)
    for name, weight in chain_weights(y, trunk).items():
        add(name, trunk_mask * weight)

    top = sorted(scores.items(), key=lambda item: item[1], reverse=True)[:4]
    total = sum(weight for _, weight in top)
    if total < 1e-8:
        top = [("Hips", 1.0)]
        total = 1.0
    normalized = [(name, weight / total) for name, weight in top]
    for name, weight in normalized:
        groups[name].add([vertex.index], weight, "REPLACE")
        used[name] += 1
    influence_histogram[len(normalized)] += 1
    actual_sum = sum(weight for _, weight in normalized)
    minimum_sum = min(minimum_sum, actual_sum)
    maximum_sum = max(maximum_sum, actual_sum)

rig["rig_stage"] = "ウェイト設定済み・アニメーション未作成"
mesh["weight_method"] = "Spatial regions, up to four normalized bones per vertex"
print({
    "vertices": len(mesh.data.vertices),
    "weighted_bones": len(used),
    "unused_deform_bones": sorted(set(groups) - set(used)),
    "influences_per_vertex": dict(influence_histogram),
    "weight_sum_range": (minimum_sum, maximum_sum),
    "bone_vertex_counts": dict(sorted(used.items())),
})
