"""Give each limb its own chain, with body blending only at its root."""


def smoothstep(a, b, x):
    t=max(0.,min(1.,(x-a)/(b-a)))
    return t*t*(3-2*t)


def apply_leg_weights(mesh):
    if mesh.get('leg_weight_fix_version') == 1:
        return 0
    groups={g.name:g for g in mesh.vertex_groups}
    names={g.index:g.name for g in mesh.vertex_groups}
    def is_limb(name):
        return name.startswith(('FrontLeg','FrontFoot','FrontToe','HindLeg','HindFoot','HindToe'))
    changed=0
    for vertex in mesh.data.vertices:
        x,y,z=vertex.co
        region='Front' if y<0 else 'Hind'
        front=region=='Front'
        root_blend=smoothstep(.215 if front else .205,.305 if front else .285,z)
        depth_root=(1-smoothstep(-.130,-.075,y)) if front else smoothstep(.080,.140,y)
        depth_paw=(1-smoothstep(-.100,-.050,y)) if front else smoothstep(.045,.100,y)
        depth=depth_paw*(1-root_blend)+depth_root*root_blend
        height=1-smoothstep(.215 if front else .205,.305 if front else .285,z)
        # Keep the belly between the limbs attached to the body. Below the
        # lower joint, each side belongs entirely to its own limb.
        lateral=((1-root_blend)*smoothstep(0,.005,abs(x))
                 +root_blend*smoothstep(.002,.035,abs(x)))
        mask=depth*height*lateral
        if mask<.001:
            continue
        old={names[g.group]:g.weight for g in vertex.groups}
        body={n:w for n,w in old.items() if not is_limb(n)}
        old_limb=sum(w for n,w in old.items() if is_limb(n))
        amount=old_limb+(1-old_limb)*mask
        side='L' if x>=0 else 'R'
        joint=.19 if front else .17
        ankle=.055 if front else .065
        # This mesh has thick legs and sparse, separated surface islands.
        # A narrow band folds the inside surface onto itself during bending.
        upper=smoothstep(joint-.120,joint+.120,z)
        foot=1-smoothstep(ankle-.025,ankle+.025,z)
        toe=(1-smoothstep(-.255,-.220,y) if front else 1-smoothstep(.140,.180,y))
        toe*=.85*(1-smoothstep(.025,.045,z))
        chain={region+'Leg_Upper.'+side:upper,
               region+'Leg_Lower.'+side:(1-upper)*(1-foot),
               region+'Foot.'+side:(1-upper)*foot*(1-toe),
               region+'Toe.'+side:(1-upper)*foot*toe}
        body_total=sum(body.values())
        weights={n:w*(1-amount)/body_total for n,w in body.items()} if body_total>1e-8 else {}
        if not weights:
            amount=1.
        for n,w in chain.items():
            weights[n]=w*amount
        weights=dict(sorted(((n,w) for n,w in weights.items() if w>1e-8),key=lambda pair:pair[1],reverse=True)[:4])
        total=sum(weights.values())
        for membership in list(vertex.groups):
            mesh.vertex_groups[membership.group].remove([vertex.index])
        for n,w in weights.items():
            groups[n].add([vertex.index],w/total,'REPLACE')
        changed+=1
    mesh['leg_weight_fix_version']=1
    return changed
