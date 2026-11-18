import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Pact/"
reset(); cam=setup_scene(res=900,samples=48)
bpy.ops.import_scene.fbx(filepath=OUT+"pact_altar.fbx")
aim(cam,(2.2,-4.0,3.0),(0,0,0.8),50); render(OUT+"preview_front.png")
