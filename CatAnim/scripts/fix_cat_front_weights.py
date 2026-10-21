"""Weights for the revised front elbow; safe to reapply after spatial binding."""


def apply_front_weights(mesh):
    changed=0
    for v in mesh.data.vertices:
        if v.co.y>-.075 or v.co.z>.305:continue
        memberships={mesh.vertex_groups[g.group].name:g.weight for g in v.groups}
        side='L' if v.co.x>=0 else 'R'
        prefix=('FrontLeg_Upper.'+side,'FrontLeg_Lower.'+side)
        total=sum(memberships.get(n,0) for n in prefix)
        if total<1e-8:continue
        # An 11 cm transition spreads the bend across the separated fur islands.
        u=max(0.,min(1.,(v.co.z-.135)/.11));u=u*u*(3-2*u)
        if sum(1 for n in memberships if n not in prefix)>=3:
            u=float(u>=.5)
        for n,w in zip(prefix,(u,1-u)):
            group=mesh.vertex_groups[n]
            if w*total>1e-8:group.add([v.index],w*total,'REPLACE')
            else:group.remove([v.index])
        changed+=1
    mesh['front_leg_weight_version']=1
    return changed
