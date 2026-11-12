import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
import numpy as np
OUT=R+"out/Boss/"
mode=sys.argv[sys.argv.index("--")+1]
parts={"head":"head","back":"chest","core":"chest","weapon":"handslot.r","shield":"handslot.l","shoulder_l":"upperarm.l","shoulder_r":"upperarm.r"}
reset(); cam=setup_scene(res=1000 if mode=="final" else 500,samples=64 if mode=="final" else 16)
arm,new=load_body(R+"src/Skeleton_Warrior.fbx",OUT+"boss_body_texture.png",["Skeleton_Warrior_Helmet","Skeleton_Warrior_Cloak"],suffix="b")
objs={p:attach(arm,OUT+f"boss_{p}.fbx",b) for p,b in parts.items()}
arm.animation_data_create()
def setact(n):
    act=[a for a in bpy.data.actions if a.name.split('.')[0]==n][-1]
    arm.animation_data.action=act; arm.animation_data.action_slot=act.slots[0]; return act
def updir(side):
    m=arm.matrix_world@arm.pose.bones["upperarm."+side].matrix; return (m.to_3x3()@Vector((0,1,0))).normalized()
def peak(n):
    act=setact(n); f0,f1=[int(x) for x in act.frame_range]; best=(-9,f0)
    for f in range(f0,f1+1):
        bpy.context.scene.frame_set(f); z=updir("r").z
        if z>best[0]: best=(z,f)
    print("PEAK",n,best,[round(x,2) for x in updir("r")],[round(x,2) for x in updir("l")]); bpy.context.scene.frame_set(best[1]); return best[1]
def show(names,vis):
    for p in names:
        for o in objs[p]: o.hide_render=not vis
shots=[]
setact("Rig|Idle"); bpy.context.scene.frame_set(10)
aim(cam,(3.4,-6.2,2.9),(0,-0.2,1.35),55); shots.append(("preview_shoulders_idle",))
if mode=="final": render(OUT+"preview_shoulders_idle.png")
else: render(OUT+"_s1.png")
peak("Rig|1H_Melee_Attack_Jump_Chop")
aim(cam,(3.4,-6.2,2.6),(0,-0.2,1.6),55)
render(OUT+("preview_shoulders_attack.png" if mode=="final" else "_s2.png"))
aim(cam,(-3.4,-6.2,2.6),(0,-0.2,1.6),55)
render(OUT+("preview_shoulders_attack_b.png" if mode=="final" else "_s3.png"))
peak("Rig|Cheer"); show(["weapon","shield"],False)
aim(cam,(2.4,-6.6,2.4),(0,0,1.6),55)
render(OUT+("preview_shoulders_cheer.png" if mode=="final" else "_s4.png"))
if mode!="final":
    rows=[np.array(bpy.data.images.load(OUT+f"_s{i}.png").pixels[:],dtype=np.float32).reshape(500,500,4) for i in (1,2,3,4)]
    m=np.concatenate(rows,axis=1); out=bpy.data.images.new("m",m.shape[1],m.shape[0],alpha=True); out.pixels=m.ravel(); out.filepath_raw=OUT+"montage.png"; out.file_format='PNG'; out.save()
