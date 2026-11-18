# Altar do Pacto: bloco de pedra escura com pano vinho caido na frente, bacia rasa com liquido escuro (Glow fraco),
# velas derretidas e uma corrente. Origem no chao, centro da base. blender -b -P pact.py
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Pact/"
def fin(name,objs):
    o=join(name,objs,38); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
def lathe(name,m,prof,segs=10,loc=(0,0,0),cap_top=True):
    bm=bmesh.new(); rings=[]
    for r,z in prof: rings.append([bm.verts.new((loc[0]+r*math.cos(2*math.pi*k/segs),loc[1]+r*math.sin(2*math.pi*k/segs),loc[2]+z)) for k in range(segs)])
    for a,b in zip(rings[:-1],rings[1:]):
        for k in range(segs): bm.faces.new((a[k],a[(k+1)%segs],b[(k+1)%segs],b[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_top: bm.faces.new(rings[-1])
    return new_obj(name,bm,m)
reset()
SD=mat("StoneDark","#4a4550"); St=mat("Stone","#625c68"); SL=mat("StoneLight","#7a7480"); Cloth=mat("Cloth","#7a2e3c"); ClothD=mat("ClothDark","#5a2230")
Gold=mat("Trim","#b08a46"); Glow=mat("Glow","#8a2238"); Wax=mat("Wax","#d8ccb4"); Flame=mat("Flame","#f0a050"); Iron=mat("Iron","#3e3c44")
P=[box("base",SD,(1.3,1.0,0.18),(0,0,0.09),bevel=0.04),
   box("block",St,(1.05,0.78,0.86),(0,0,0.61),bevel=0.05,taper=(0.94,0.94)),
   box("top",SL,(1.25,0.95,0.14),(0,0,1.1),bevel=0.04),
   box("trimF",Gold,(1.06,0.03,0.06),(0,-0.4,0.98)),
   # pano vinho caindo pela frente, com ponta em V
   box("cloth",Cloth,(0.62,0.04,0.72),(0,-0.48,0.74),bevel=0.01),
   box("clothTop",Cloth,(0.62,0.5,0.03),(0,-0.22,1.18))]
# bacia rasa com liquido escuro
P.append(lathe("bowl",SD,[(0.16,0),(0.3,0.06),(0.34,0.16),(0.3,0.18),(0.27,0.1)],12,(0,0.05,1.17),cap_top=False))
P.append(lathe("liquid",Glow,[(0.27,0.12),(0.27,0.121)],12,(0,0.05,1.17)))
# velas derretidas nos cantos de tras
for x,h in ((-0.48,0.26),(0.46,0.18),(0.36,0.12)):
    y=0.3 if x<0.4 else 0.18
    P.append(lathe("candle",Wax,[(0.07,0),(0.065,h),(0.04,h+0.02)],8,(x,y,1.17)))
    P.append(lathe("flame",Flame,[(0.0,h+0.03),(0.03,h+0.07),(0.0,h+0.14)],6,(x,y,1.17)))
# corrente pendurada do lado
for k in range(5):
    P.append(box("link",Iron,(0.05,0.1,0.14),(0.6,-0.1+0.0*k,1.05-k*0.13),rot=(0,0,90 if k%2 else 0),bevel=0.01))
fin("pact_altar",P)
