import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Rest/"
reset(); cam=setup_scene(res=900,samples=48)
for nm,x in (("rest_campfire",0.0),("rest_log",0.0)):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=OUT+nm+".fbx")
    for o in set(bpy.data.objects)-before:
        if nm=="rest_log": o.location=(0,1.7,0)
aim(cam,(0,-5.2,6.0),(0,0.4,0.3),50); render(OUT+"preview_top.png")
