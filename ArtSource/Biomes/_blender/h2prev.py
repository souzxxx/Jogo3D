import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Biomes/Heart/"
reset(); cam=setup_scene(res=900,samples=48)
for nm,x in (("heart_altar",-1.4),("heart_vent",1.4)):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=OUT+nm+".fbx")
    for o in set(bpy.data.objects)-before: o.location.x+=x
aim(cam,(0,-5.5,4.2),(0,0,0.4),45); render(OUT+"preview2.png")
