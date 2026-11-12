import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
from mathutils.bvhtree import BVHTree
O=R+"out/Boss2/"
reset()
arm,new=load_body(R+"src/Skeleton_Mage.fbx",None,["Skeleton_Mage_Hat"],suffix="w")
parts={}
for p,b in (("head","head"),("mantle","chest"),("core","chest"),("staff","handslot.r"),("shards","chest"),("shoulder_l","upperarm.l"),("shoulder_r","upperarm.r")): parts[p]=attach(arm,O+f"weaver_{p}.fbx",b)[0]
body={n:[o for o in new if o.name.startswith(n)][0] for n in ("Skeleton_Mage_Skull","Skeleton_Mage_Jaw","Skeleton_Mage_Body","Skeleton_Mage_ArmLeft","Skeleton_Mage_ArmRight")}
arm.animation_data_create()
def tree(o):
    dg=bpy.context.evaluated_depsgraph_get(); return BVHTree.FromObject(o,dg)
def check(label):
    T={k:tree(o) for k,o in list(parts.items())+list(body.items())}
    res=[]
    for a in ("staff","mantle","shards","head","core","shoulder_l","shoulder_r"):
        for b in ("Skeleton_Mage_Skull","Skeleton_Mage_Jaw","Skeleton_Mage_Body","Skeleton_Mage_ArmLeft","Skeleton_Mage_ArmRight"):
            n=len(T[a].overlap(T[b]))
            if n: res.append(f"{a}x{b.replace('Skeleton_Mage_','')}:{n}")
    print("CHECK",label," ".join(res) or "none")
for actn,frames in (("Rig|Idle",[10]),("Rig|Spellcast_Raise",[10,21,35]),("Rig|Spellcast_Summon",[40,101])):
    act=[a for a in bpy.data.actions if a.name.split('.')[0]==actn][-1]; arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
    for f in frames: bpy.context.scene.frame_set(f); check(f"{actn}@{f}")
