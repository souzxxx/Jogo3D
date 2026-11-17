import sys; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Merchant/"
reset(); cam=setup_scene(res=900,samples=48)
arm,new=load_body(R+"src/Mage.fbx",None,["Mage_Hat","Mage_Cape"])
for f,b in (("merchant_hat","head"),("merchant_pack","chest"),("merchant_pouch","hips")): attach(arm,OUT+f+".fbx",b)
aim(cam,(2.6,-4.8,2.6),(0,0,1.2),50); render(OUT+"preview_front.png")
aim(cam,(-2.6,4.8,2.6),(0,0,1.2),50); render(OUT+"preview_back.png")
aim(cam,(0,-5.5,7.5),(0,0,1.0),50); render(OUT+"preview_top.png")
