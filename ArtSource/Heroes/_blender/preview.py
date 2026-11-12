import sys, json; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/"
CFG={
 "Boss":dict(body="Skeleton_Warrior",hide=["Skeleton_Warrior_Helmet","Skeleton_Warrior_Cloak"],parts={"head":"head","shoulders":"chest","back":"chest","core":"chest","weapon":"handslot.r","shield":"handslot.l"},action="Rig|Idle"),
 "Rax":dict(body="Rogue",hide=["Rogue_Head","Rogue_Cape"],parts={"head":"head","tail":"hips","weapon_l":"handslot.l","weapon_r":"handslot.r"},action="Rig|Unarmed_Idle"),
 "Liora":dict(body="Mage",hide=["Mage_Head","Mage_Hat","Mage_Cape"],parts={"head":"head","tail":"hips","weapon":"handslot.r"},action="Rig|Idle"),
 "Brann":dict(body="Barbarian",hide=["Barbarian_Head","Barbarian_Hat","Barbarian_Cape"],parts={"head":"head","tail":"hips","weapon":"handslot.r"},action="Rig|2H_Melee_Idle"),
 "Vey":dict(body="RogueHooded",hide=["Rogue_LegLeft","Rogue_LegRight"],parts={"head":"head","tail":"hips","weapon_l":"handslot.l","weapon_r":"handslot.r"},action="Rig|Idle"),
}
def build(hero,offset=(0,0,0),tpose=False,scale=1.0):
    c=CFG[hero]
    arm,new=load_body(R+f"src/{c['body']}.fbx",OUT+f"{hero}/{hero.lower()}_body_texture.png",c["hide"],suffix=hero)
    for part,bone in c["parts"].items(): attach(arm,OUT+f"{hero}/{hero.lower()}_{part}.fbx",bone)
    if not tpose:
        # private copy of the action per armature is not needed: each import brings its own actions (suffix .001 etc.)
        acts=[a for a in bpy.data.actions if a.name.split('.')[0]==c["action"]]
        act=acts[-1]
        arm.animation_data_create(); arm.animation_data.action=act
        try: arm.animation_data.action_slot=act.slots[0]
        except Exception as e: print("slot",e)
        # the action may animate the armature object itself; keep it under the Root empty
    arm.parent.location=offset; arm.parent.scale=(scale,)*3
    return arm
args=sys.argv[sys.argv.index("--")+1:]
mode=args[0]
if mode=="boss":
    reset(); cam=setup_scene(res=1000,samples=64)
    build("Boss",tpose=(len(args)>1)); bpy.context.scene.frame_set(10)
    sfx="_tpose" if len(args)>1 else ""
    for vn,(loc,tgt,lens) in {"front":((3.8,-7.0,3.6),(0,-0.3,1.4),62),"back":((-3.8,7.0,3.9),(0,0.1,1.4),62),"top":((0,-7.46,11.65),(0,-0.3,1.2),72)}.items():
        aim(cam,loc,tgt,lens); render(OUT+f"Boss/preview_{vn}{sfx}.png")
elif mode=="scale":
    reset(); cam=setup_scene(res=1400,samples=64)
    build("Brann",offset=(-3.6,-0.5,0)); build("Boss",offset=(1.6,0,0),scale=2.3); bpy.context.scene.frame_set(10)
    bpy.context.scene.render.resolution_y=1000
    aim(cam,(2.0,-19,9.0),(-0.6,0,2.4),50); render(OUT+"Boss/scale.png")
    aim(cam,(-0.6,-12.2,19.0),(-0.6,-0.6,2.0),50); render(OUT+"Boss/scale_top.png")
elif mode=="lineup":
    reset(); cam=setup_scene(res=1800,samples=64)
    for i,h in enumerate(["Rax","Liora","Brann","Vey"]): build(h,offset=((i-1.5)*3.0,0,0))
    bpy.context.scene.frame_set(10)
    bpy.context.scene.render.resolution_y=800
    aim(cam,(0,-17,6.5),(0,0,1.25),58); render(OUT+"lineup.png")
    aim(cam,(0,-10.3,15.6),(0,0.3,1.0),58); render(OUT+"lineup_top.png")
else:
    for h in args[1:]:
        reset(); cam=setup_scene(res=900,samples=48)
        build(h,tpose=(mode=="tpose"))
        bpy.context.scene.frame_set(10)
        for vn,(loc,tgt,lens) in VIEWS.items():
            aim(cam,loc,tgt,lens); render(OUT+f"{h}/preview_{vn}{'_tpose' if mode=='tpose' else ''}.png")
