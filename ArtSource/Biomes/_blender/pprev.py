import sys, json; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Biomes/"
ORDER={"Crypt":["crypt_sarcophagus","crypt_urns","crypt_statue","crypt_pool","crypt_stalagmites"],"Garden":["garden_tree","garden_mushrooms","garden_roots","garden_planter","garden_statue"],"Ash":["ash_brazier","ash_beams","ash_anvil","ash_cage"],"Heart":["heart_spire","heart_crystals"]}
for b,names in ORDER.items():
    reset(); cam=setup_scene(res=1800,samples=64)
    bpy.data.materials["FloorMat"].node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value=hexc("#5d586a")
    n=len(names)+1; sp=3.0
    for i,nm in enumerate(names):
        before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=OUT+f"{b}/{nm}.fbx")
        for o in set(bpy.data.objects)-before: o.location.x+=(i-(n-1)/2)*sp
    load_body(R+"src/Mage.fbx",None,["Mage_Hat"],offset=((n-1-(n-1)/2)*sp,0,0),suffix="m")
    act=[a for a in bpy.data.actions if a.name=="Rig|Idle"][0]
    arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; arm.animation_data_create(); arm.animation_data.action=act
    try: arm.animation_data.action_slot=act.slots[0]
    except Exception as e: print("slot",e)
    bpy.context.scene.frame_set(10)
    W=n*sp; bpy.context.scene.render.resolution_y=int(1800*max(0.42,min(0.8,5.5/W)))
    d=W*1.25+2
    aim(cam,(0,-d*0.5736,d*0.8192+1),(0,-0.1,0.9),50); render(OUT+f"{b}/preview_top.png")
    aim(cam,(d*0.38,-d*0.8,d*0.42),(0,0,1.0),50); render(OUT+f"{b}/preview_front.png")
