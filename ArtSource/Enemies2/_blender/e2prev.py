import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
O=R+"out/"
E2=O+"Enemies2/"; E1=O+"Enemies/"
CFG={
 "Rastejante":dict(body="Skeleton_Minion",hide=[],tint=None,scale=1.0,parts={}),
 "Cinzento":dict(body="Skeleton_Minion",hide=[],tint=(0.62,0.6,0.6),scale=0.65,parts={E2+"cinzento_head.fbx":"head",E2+"cinzento_back.fbx":"chest",E2+"cinzento_weapon.fbx":"handslot.r"}),
 "Brasa":dict(body="Skeleton_Warrior",hide=["Skeleton_Warrior_Helmet","Skeleton_Warrior_Cloak"],tint=(0.9,0.55,0.4),scale=1.1,parts={E2+"brasa_head.fbx":"head",E2+"brasa_body.fbx":"chest",E2+"brasa_shoulder_l.fbx":"upperarm.l",E2+"brasa_shoulder_r.fbx":"upperarm.r",E2+"brasa_weapon.fbx":"handslot.r"}),
 "Sentinela":dict(body="Skeleton_Mage",hide=["Skeleton_Mage_Hat"],tint=(0.62,0.72,0.6),scale=1.25,parts={E2+"sentinela_head.fbx":"head",E2+"sentinela_back.fbx":"chest",E2+"sentinela_base.fbx":"root",E2+"sentinela_weapon.fbx":"handslot.r"}),
 "Ceifador":dict(body="Skeleton_Rogue",hide=["Skeleton_Rogue_Hood","Skeleton_Rogue_Cape"],tint=(0.62,0.68,0.82),scale=1.12,parts={E1+"ceifador_head.fbx":"head",E1+"ceifador_weapon.fbx":"handslot.r",E1+"ceifador_back.fbx":"chest"},action="Rig|2H_Melee_Idle"),
 "Bruto":dict(body="Skeleton_Warrior",hide=[],tint=None,scale=1.3,parts={},props=True),
}
def build(k,offset=(0,0,0),frame=10):
    c=CFG[k]
    arm,new=load_body(R+f"src/{c['body']}.fbx",None,c["hide"],suffix=k,props=c.get("props",False),tint=c["tint"])
    for o in new:
        if o.type=='MESH' and o.name.split('.')[0] in c["hide"]: o.hide_render=True
    for f,b in c["parts"].items(): attach(arm,f,b)
    an=c.get("action","Rig|Idle"); act=[a for a in bpy.data.actions if a.name.split('.')[0]==an][-1]
    arm.animation_data_create(); arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]
    arm.parent.location=offset; arm.parent.scale=(c["scale"],)*3
mode=sys.argv[sys.argv.index("--")+1]
if mode=="each":
    for k in ("Cinzento","Brasa","Sentinela"):
        reset(); cam=setup_scene(res=900,samples=48); build(k); bpy.context.scene.frame_set(10)
        sc=CFG[k]["scale"]
        aim(cam,(3.6*sc,-6.6*sc,3.5*sc),(0,-0.1,1.4*sc),55); render(O+f"Enemies2/preview_{k.lower()}_front.png")
        aim(cam,(0,-7.46*sc,11.65*sc),(0,-0.1,1.1*sc),62); render(O+f"Enemies2/preview_{k.lower()}_top.png")
elif mode=="lineup":
    reset(); cam=setup_scene(res=2100,samples=64)
    ks=["Rastejante","Cinzento","Brasa","Sentinela","Ceifador","Bruto"]
    for i,k in enumerate(ks): build(k,offset=((i-2.5)*3.0,0,0))
    bpy.context.scene.frame_set(10); bpy.context.scene.render.resolution_y=640
    aim(cam,(0,-14.6,22.1),(0,-0.3,1.0),48); render(O+"Enemies2/lineup_top.png")
    aim(cam,(0,-24,8.5),(0,0,1.3),46); render(O+"Enemies2/lineup_front.png")
