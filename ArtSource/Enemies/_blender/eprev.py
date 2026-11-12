import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Enemies/"
CFG={
 "Ceifador":dict(body="Skeleton_Rogue",hide=["Skeleton_Rogue_Hood","Skeleton_Rogue_Cape"],tint=(0.62,0.68,0.82),parts={"ceifador_head":"head","ceifador_weapon":"handslot.r","ceifador_back":"chest"},action="Rig|2H_Melee_Idle"),
 "Arremessador":dict(body="Skeleton_Minion",hide=[],tint=(0.78,0.8,0.56),parts={"arremessador_head":"head","arremessador_back":"chest","arremessador_weapon":"handslot.r"},action="Rig|Idle"),
 "Xama":dict(body="Skeleton_Mage",hide=["Skeleton_Mage_Hat"],tint=(0.56,0.82,0.7),parts={"xama_head":"head","xama_weapon":"handslot.r","xama_back":"chest"},action="Rig|Idle"),
}
for n in ("Minion","Mage","Warrior","Rogue"):
    CFG[n]=dict(body="Skeleton_"+n,hide=[],tint=None,parts={},action="Rig|Idle",props=True)
    CFG["Campeao"+n]=dict(body="Skeleton_"+n,hide=[],tint=None,parts={"campeao_crown":"head"},action="Rig|Idle",props=True)
def build(k,offset=(0,0,0),scale=1.0):
    c=CFG[k]
    arm,new=load_body(R+f"src/{c['body']}.fbx",None,c["hide"],suffix=k,props=c.get("props",False),tint=c["tint"])
    for o in new:
        if o.type=='MESH' and 'Cape' in o.name and o.name.split('.')[0] in c["hide"]: o.hide_render=True
    for part,bone in c["parts"].items(): attach(arm,OUT+part+".fbx",bone)
    act=[a for a in bpy.data.actions if a.name.split('.')[0]==c["action"]][-1]
    arm.animation_data_create(); arm.animation_data.action=act
    try: arm.animation_data.action_slot=act.slots[0]
    except Exception as e: print("slot",e)
    arm.parent.location=offset; arm.parent.scale=(scale,)*3
mode=sys.argv[sys.argv.index("--")+1]
if mode=="each":
    for k in ("Ceifador","Arremessador","Xama"):
        reset(); cam=setup_scene(res=900,samples=48); build(k); bpy.context.scene.frame_set(10)
        aim(cam,(3.6,-6.6,3.5),(0,-0.1,1.45),68); render(OUT+f"preview_{k.lower()}_front.png")
        aim(cam,(0,-7.46,11.65),(0,-0.1,1.2),80); render(OUT+f"preview_{k.lower()}_top.png")
elif mode=="lineup":
    reset(); cam=setup_scene(res=2100,samples=64)
    ks=["Minion","Mage","Warrior","Rogue","Ceifador","Arremessador","Xama"]
    for i,k in enumerate(ks): build(k,offset=((i-3)*2.65,0,0))
    bpy.context.scene.frame_set(10); bpy.context.scene.render.resolution_y=640
    aim(cam,(0,-14.6,22.1),(0,-0.3,1.0),50); render(OUT+"lineup_top.png")
    aim(cam,(0,-24,8.5),(0,0,1.3),48); render(OUT+"lineup_front.png")
elif mode=="campeao":
    reset(); cam=setup_scene(res=1600,samples=64)
    for i,n in enumerate(("Minion","Mage","Warrior","Rogue")): build("Campeao"+n,offset=((i-1.5)*3.6,0,0),scale=1.3)
    bpy.context.scene.frame_set(10); bpy.context.scene.render.resolution_y=1000
    aim(cam,(0,-18,8.0),(0,0,1.9),50); render(OUT+"campeao.png")
    aim(cam,(0,-10.9,17.5),(0,-0.2,1.6),50); render(OUT+"campeao_top.png")
