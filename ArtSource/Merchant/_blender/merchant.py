# Mercador da loja: chapeu de aba larga com pena, mochila de mascate (cobertor enrolado, panela, pergaminhos) e bolsa de moedas.
# Pecas no espaco do corpo Mage.fbx em repouso (ver ArtSource/Heroes/README.md). blender -b -P merchant.py
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Merchant/"
def fin(name,objs):
    o=join(name,objs,38); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
def cyl(name,m,r,z0,z1,loc=(0,0),segs=12,r2=None,scale=(1,1)):
    bm=bmesh.new(); r2=r if r2 is None else r2
    bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=segs,radius1=r,radius2=r2,depth=z1-z0)
    for v in bm.verts: v.co.x*=scale[0]; v.co.y*=scale[1]; v.co.z+=(z0+z1)/2; v.co.x+=loc[0]; v.co.y+=loc[1]
    return new_obj(name,bm,m)
reset()
Felt=mat("Felt","#5b3f33"); FeltD=mat("FeltDark","#46302a"); Band=mat("Band","#c8963e"); Feather=mat("Feather","#b9523a"); FeatherL=mat("FeatherLight","#e0a070")
cy=0.11
P=[cyl("brim",Felt,0.86,1.86,1.92,(0,cy),segs=16,scale=(1,0.92)),
   cyl("brimT",FeltD,0.84,1.92,1.93,(0,cy),segs=16,scale=(1,0.92)),
   cyl("crown",Felt,0.6,1.92,2.36,(0,cy),segs=14,r2=0.5),
   cyl("top",FeltD,0.5,2.36,2.42,(0,cy),segs=14,r2=0.44),
   cyl("band",Band,0.61,1.93,2.03,(0,cy),segs=14,r2=0.59)]
# pena presa na fita, inclinada para tras
P.append(tube("quill",Feather,[(0.5,cy+0.1,2.0),(0.62,cy+0.45,2.35),(0.66,cy+0.8,2.62)],[(0.09,0.02),(0.12,0.02),(0.03,0.01)],sides=4,up=(1,0,0)))
P.append(tube("vane",FeatherL,[(0.52,cy+0.14,2.04),(0.6,cy+0.42,2.3)],[(0.05,0.015),(0.02,0.01)],sides=4,up=(1,0,0)))
fin("merchant_hat",P)
reset()
Canvas=mat("Canvas","#b39c74"); CanvasD=mat("CanvasDark","#8f7a58"); Leather=mat("Leather","#7a4e33"); Roll=mat("Blanket","#a4483a"); RollL=mat("BlanketLight","#d08a5a")
Metal=mat("Metal","#6c6d78"); Wood=mat("Wood","#8a5a3c"); Paper=mat("Paper","#e6d6ae")
P=[rbox("pack",Canvas,(0.74,0.42,0.72),(0,0.6,0.96),cuts=2,round=0.5),
   rbox("flap",CanvasD,(0.76,0.44,0.2),(0,0.6,1.26),cuts=2,round=0.5),
   rbox("pocket",CanvasD,(0.46,0.12,0.28),(0,0.84,0.86),cuts=2,round=0.5)]
for sx in (-0.2,0.2):
    P.append(box("strap",Leather,(0.08,0.03,0.6),(sx,0.38,1.02)))
    P.append(box("strapF",Leather,(0.07,0.03,0.42),(sx,-0.39,1.0)))
# cobertor enrolado em cima
P.append(cyl("roll",Roll,0.15,-0.46,0.46,(0,0),segs=10))
P[-1].data.transform(Matrix.Translation((0,0.6,1.48))@Euler((0,math.radians(90),0)).to_matrix().to_4x4()); P[-1].data.update()
for x in (-0.47,0.47):
    c=cyl("rollEnd",RollL,0.12,0,0.02,(0,0),segs=10); c.data.transform(Matrix.Translation((x,0.6,1.48))@Euler((0,math.radians(90),0)).to_matrix().to_4x4()); c.data.update(); P.append(c)
# panela pendurada do lado esquerdo e pergaminhos saindo de cima
pan=cyl("pan",Metal,0.17,0,0.12,(0,0),segs=10)
pan.data.transform(Matrix.Translation((0.43,0.62,0.86))@Euler((0,math.radians(90),0)).to_matrix().to_4x4()); pan.data.update(); P.append(pan)
P.append(box("handle",Wood,(0.04,0.04,0.3),(0.47,0.62,1.08)))
for k,(x,y,t) in enumerate(((-0.18,0.52,8),(-0.08,0.66,-6),(0.1,0.56,12))):
    s=cyl("scroll",Paper,0.045,1.3,1.62,(x,y),segs=6); s.data.transform(Matrix.Translation((x,y,1.3))@Euler((math.radians(t),0,0)).to_matrix().to_4x4()@Matrix.Translation((-x,-y,-1.3))); s.data.update(); P.append(s)
fin("merchant_pack",P)
reset()
Pouch=mat("Pouch","#8a5f38"); Tie=mat("Tie","#c8963e"); Coin=mat("Coin","#e0b24a")
P=[ico("pouch",Pouch,(0.24,0.2,0.26),(0.3,-0.36,0.62),sub=2), cyl("neck",Pouch,0.05,0.72,0.8,(0.3,-0.36),segs=8,r2=0.07),
   cyl("tie",Tie,0.065,0.72,0.75,(0.3,-0.36),segs=8)]
for k in range(3): P.append(cyl("coin",Coin,0.05,0.79+k*0.0,0.81,(0.27+k*0.03,-0.38+k*0.02),segs=8))
fin("merchant_pouch",P)
