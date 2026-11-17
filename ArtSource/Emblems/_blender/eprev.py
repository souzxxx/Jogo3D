import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Emblems/"
reset(); cam=setup_scene(res=1200,samples=48)
bpy.data.materials["FloorMat"].node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value=hexc("#4b4450")
for i,n in enumerate(["emblem_fire","emblem_water","emblem_earth","emblem_air"]):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=OUT+n+".fbx")
    for o in set(bpy.data.objects)-before: o.location.x+=(i-1.5)*0.95
bpy.context.scene.render.resolution_y=520
aim(cam,(0.5,-5.6,2.1),(0,0,0.36),50); render(OUT+"preview_front.png")
