"""Restrict ear deformation and make the face follow Head.

Run apply_face_weights(mesh) after the initial spatial weighting, or use runpy
to load this file when repairing an existing mesh. Does not alter animation.
"""


def smoothstep(start, end, value):
    t = max(0.0, min(1.0, (value - start) / (end - start)))
    return t * t * (3.0 - 2.0 * t)


def apply_face_weights(mesh):
    if mesh.get('face_weight_fix_version') == 1:
        return 0
    groups = {group.name: group for group in mesh.vertex_groups}
    names = {group.index: group.name for group in mesh.vertex_groups}
    changed = 0
    for vertex in mesh.data.vertices:
        x, y, z = vertex.co
        weights = {names[g.group]: g.weight for g in vertex.groups if g.weight > 0}
        before = weights.copy()

        # Original ear weights start at z=.485, inside the upper face. Keep
        # deformation above the ear root with a soft transition to the tip.
        old_ear = smoothstep(.485, .535, z)
        new_ear = smoothstep(.520, .550, z)
        keep = new_ear / old_ear if old_ear > 0 else 0.0
        for name in ('Ear_01.L', 'Ear_02.L', 'Ear_01.R', 'Ear_02.R'):
            weight = weights.get(name, 0.0)
            retained = weight * keep
            weights[name] = retained
            weights['Head'] = weights.get('Head', 0.0) + weight - retained

        # Use height as well as depth: the old depth-only assignment puts
        # Neck_02 on cheeks. Blend at the underside/back of the head instead.
        front = (1.0 - smoothstep(-.280, -.200, y)) * smoothstep(.300, .350, z)
        crown = (1.0 - smoothstep(-.200, -.150, y)) * smoothstep(.430, .490, z)
        head = max(front, crown)
        for name in list(weights):
            if name.startswith(('Neck_', 'Spine_')) or name == 'Hips':
                moved = weights[name] * head
                weights[name] -= moved
                weights['Head'] = weights.get('Head', 0.0) + moved

        weights = {name: weight for name, weight in weights.items() if weight > 1e-8}
        # Adding Head can create a fifth influence on a transition vertex.
        weights = dict(sorted(weights.items(), key=lambda pair: pair[1], reverse=True)[:4])
        total = sum(weights.values())
        weights = {name: weight / total for name, weight in weights.items()}
        if set(weights) != set(before) or any(abs(w - before.get(n, 0)) > 1e-7 for n, w in weights.items()):
            for membership in list(vertex.groups):
                mesh.vertex_groups[membership.group].remove([vertex.index])
            for name, weight in weights.items():
                groups[name].add([vertex.index], weight, 'REPLACE')
            changed += 1
    mesh['weight_method'] = 'Spatial regions; face follows Head; ear roots limited; max four normalized bones'
    mesh['face_weight_fix_version'] = 1
    return changed
