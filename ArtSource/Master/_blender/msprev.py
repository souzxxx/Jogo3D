import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Master/"
reset(); cam=setup_scene(res=900,samples=48)
arm,new=load_body(R+"src/Mage.fbx",None,[],props=True)
for o in new:
    if o.name.split('.')[0] in ("1H_Wand","Spellbook_open"): o.hide_render=True
attach(arm,OUT+"master_beard.fbx","head")
aim(cam,(2.6,-4.8,2.6),(0,0,1.2),50); render(OUT+"preview_front.png")
aim(cam,(0,-5.5,7.5),(0,0,1.0),50); render(OUT+"preview_top.png")
