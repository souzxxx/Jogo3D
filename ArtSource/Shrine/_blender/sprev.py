import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Shrine/"
reset(); cam=setup_scene(res=900,samples=48)
bpy.ops.import_scene.fbx(filepath=OUT+"shrine_pedestal.fbx")
aim(cam,(2.0,-3.6,2.8),(0,0,0.7),50); render(OUT+"preview_front.png")
