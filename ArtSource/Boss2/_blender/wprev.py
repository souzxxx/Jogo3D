import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
import numpy as np
O=R+"out/"
CFG={
 "Weaver":dict(body="Skeleton_Mage",tex=O+"Boss2/weaver_body_texture.png",hide=["Skeleton_Mage_Hat"],parts={O+"Boss2/weaver_head.fbx":"head",O+"Boss2/weaver_mantle.fbx":"chest",O+"Boss2/weaver_core.fbx":"chest",O+"Boss2/weaver_staff.fbx":"handslot.r",O+"Boss2/weaver_shards.fbx":"chest",O+"Boss2/weaver_shoulder_l.fbx":"upperarm.l",O+"Boss2/weaver_shoulder_r.fbx":"upperarm.r"}),
 "Guardian":dict(body="Skeleton_Warrior",tex=O+"Boss/boss_body_texture.png",hide=["Skeleton_Warrior_Helmet","Skeleton_Warrior_Cloak"],parts={O+"Boss/boss_head.fbx":"head",O+"Boss/boss_back.fbx":"chest",O+"Boss/boss_core.fbx":"chest",O+"Boss/boss_weapon.fbx":"handslot.r",O+"Boss/boss_shield.fbx":"handslot.l",O+"Boss/boss_shoulder_l.fbx":"upperarm.l",O+"Boss/boss_shoulder_r.fbx":"upperarm.r"}),
 "Liora":dict(body="Mage",tex=O+"Liora/liora_body_texture.png",hide=["Mage_Head","Mage_Hat","Mage_Cape"],parts={O+"Liora/liora_head.fbx":"head",O+"Liora/liora_tail.fbx":"hips",O+"Liora/liora_weapon.fbx":"handslot.r"}),
}
def build(k,offset=(0,0,0),scale=1.0,action="Rig|Idle",frame=10):
    c=CFG[k]
    arm,new=load_body(R+f"src/{c['body']}.fbx",c["tex"],c["hide"],suffix=k)
    objs={}
    for f,b in c["parts"].items(): objs[f.split('/')[-1]]=attach(arm,f,b)
    arm.animation_data_create()
    act=[a for a in bpy.data.actions if a.name.split('.')[0]==action][-1]
    arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
    arm.parent.location=offset; arm.parent.scale=(scale,)*3
    return arm,act,objs
mode=sys.argv[sys.argv.index("--")+1]
if mode=="views":
    reset(); cam=setup_scene(res=1000,samples=64); arm,act,_=build("Weaver"); bpy.context.scene.frame_set(10)
    for vn,(loc,tgt,lens) in {"front":((4.2,-7.6,3.8),(0,-0.3,1.6),55),"back":((-4.2,7.6,4.0),(0,0.2,1.6),55),"top":((0,-8.2,12.8),(0,-0.3,1.3),62)}.items():
        aim(cam,loc,tgt,lens); render(O+f"Boss2/preview_{vn}.png")
elif mode=="attack":
    reset(); cam=setup_scene(res=1000,samples=64); arm,act,_=build("Weaver",action="Rig|Spellcast_Raise")
    f0,f1=[int(x) for x in act.frame_range]; best=(-9,f0)
    for f in range(f0,f1+1):
        bpy.context.scene.frame_set(f); z=(arm.matrix_world@arm.pose.bones["hand.r"].head).z
        if z>best[0]: best=(z,f)
    print("RAISE peak",best,f0,f1); bpy.context.scene.frame_set(best[1])
    aim(cam,(4.2,-7.6,3.2),(0,-0.3,1.8),52); render(O+"Boss2/preview_attack.png")
    aim(cam,(-4.4,-7.0,3.2),(0,-0.3,1.8),52); render(O+"Boss2/preview_attack_b.png")
    act2=[a for a in bpy.data.actions if a.name.split('.')[0]=="Rig|Spellcast_Summon"][-1]; arm.animation_data.action=act2; arm.animation_data.action_slot=act2.slots[0]
    f0,f1=[int(x) for x in act2.frame_range]; best=(-9,f0)
    for f in range(f0,f1+1):
        bpy.context.scene.frame_set(f); z=(arm.matrix_world@arm.pose.bones["hand.r"].head).z
        if z>best[0]: best=(z,f)
    print("SUMMON peak",best,f0,f1); bpy.context.scene.frame_set(best[1])
    aim(cam,(4.2,-7.6,3.2),(0,-0.3,1.8),52); render(O+"Boss2/preview_attack_summon.png")
elif mode=="compare":
    reset(); cam=setup_scene(res=1800,samples=64)
    build("Guardian",offset=(-3.6,0,0),scale=2.3); build("Liora",offset=(0.2,-0.6,0)); build("Weaver",offset=(3.6,0,0),scale=2.2)
    bpy.context.scene.frame_set(10); bpy.context.scene.render.resolution_y=1000
    aim(cam,(0,-21,10.5),(0,0,2.6),50); render(O+"Boss2/compare.png")
    aim(cam,(0,-14.5,22.5),(0,-0.4,2.0),50); render(O+"Boss2/compare_top.png")
