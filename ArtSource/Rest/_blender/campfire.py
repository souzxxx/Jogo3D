# Fogueira da sala de Descanso: anel de pedras, lenha em pe e brasa (Glow). Mesmas convencoes de ArtSource/Biomes.
# blender -b -P campfire.py   (lib.py e props.py em /home/souzx/quint_heroes)
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Rest/"
def rock(name,m,size,loc,seed=0,rot=(0,0,0),sub=1,j=0.22):
    rng=random.Random(seed)
    return ico(name,m,size,loc,rot=rot,sub=sub,fn=lambda v: v*(1+rng.uniform(-j,j)))
def fin(name,objs):
    o=join(name,objs,38); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
reset()
St=mat("Stone","#8a8378"); SD=mat("StoneDark","#5f5a52"); Wood=mat("Wood","#8a5a3c"); WoodL=mat("WoodLight","#b48a5e"); Burnt=mat("Burnt","#3a2e28"); Glow=mat("Glow","#f08a3c"); Ash=mat("Ash","#4a4440")
P=[rbox("ash",Ash,(1.25,1.25,0.08),(0,0,0.02),cuts=2,round=0.9)]
rg=random.Random(5)
for k in range(10):
    a=2*math.pi*k/10+rg.uniform(-0.08,0.08); d=0.78
    P.append(rock("stone",St if k%3 else SD,(rg.uniform(0.34,0.42),rg.uniform(0.28,0.34),rg.uniform(0.22,0.3)),(d*math.cos(a),d*math.sin(a),0.1),seed=k,rot=(0,0,math.degrees(a))))
# lenha em pe, encostada no centro
for k in range(5):
    a=2*math.pi*k/5+0.3; base=(0.5*math.cos(a),0.5*math.sin(a),0.04); top=(0.06*math.cos(a+0.6),0.06*math.sin(a+0.6),0.78)
    P.append(tube("log",Wood if k%2 else Burnt,[base,top],[0.075,0.05],sides=6))
    P.append(lathe("cut",WoodL,[(0.0,0),(0.05,0)],6,top) if False else rock("tip",Burnt,(0.1,0.1,0.08),top,seed=40+k))
# brasa no meio e dois tocos caidos
P+=[rock("emb",Glow,(0.5,0.5,0.2),(0,0,0.1),seed=30), rock("emb2",Glow,(0.22,0.2,0.12),(0.22,-0.12,0.16),seed=31), rock("coal",Burnt,(0.24,0.2,0.14),(-0.2,0.14,0.14),seed=32)]
P.append(tube("lay",Burnt,[(-0.35,-0.3,0.12),(0.3,-0.05,0.14)],[0.07,0.065],sides=6))
fin("rest_campfire",P)
# tronco para sentar
reset()
Wood=mat("Wood","#8a5a3c"); WoodL=mat("WoodLight","#b48a5e"); Bark=mat("Bark","#5e4030"); Moss=mat("Moss","#5d7d44")
P=[tube("trunk",Bark,[(-0.8,0,0.22),(0.8,0,0.24)],[0.22,0.2],sides=8)]
for x,r in ((-0.8,0.21),(0.8,0.19)):
    P.append(cone("ring",WoodL,r1=r*0.95,r2=r*0.95,depth=0.02,loc=(x+(-0.01 if x<0 else 0.0),0,0.23),rot=(0,90,0),segs=8))
P+=[rock("moss",Moss,(0.4,0.2,0.08),(0.2,0.05,0.44),seed=3,j=0.12)]
fin("rest_log",P)
