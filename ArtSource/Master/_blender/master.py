# Mestre das bencaos: barba longa branca com bigode e sobrancelhas grossas. Espaco do corpo Mage.fbx em repouso.
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Master/"
def fin(name,objs):
    o=join(name,objs,38); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
def blob(name,m,size,loc,seed=0,j=0.08,rot=(0,0,0)):
    rng=random.Random(seed); return ico(name,m,size,loc,rot=rot,sub=2,fn=lambda v: v*(1+rng.uniform(-j,j)))
reset()
W=mat("Beard","#e8e4dc"); WD=mat("BeardShadow","#bdb6aa")
P=[]
# barba: gotas empilhadas do queixo ate o peito, afinando na ponta
for k,(z,w,d,y) in enumerate(((1.36,0.86,0.5,-0.42),(1.2,0.74,0.42,-0.47),(1.04,0.58,0.36,-0.5),(0.9,0.42,0.3,-0.5),(0.79,0.24,0.2,-0.49))):
    P.append(blob("b",W if k%2==0 else WD,(w,d,0.3),(0,y,z),seed=k))
# bigode
for sx in (-1,1):
    P.append(blob("m",W,(0.36,0.16,0.12),(sx*0.17,-0.6,1.47),seed=10+sx,rot=(0,sx*-18,0)))
# sobrancelhas grossas
for sx in (-1,1):
    P.append(blob("e",W,(0.24,0.1,0.08),(sx*0.2,-0.56,1.78),seed=20+sx,rot=(0,sx*12,0)))
fin("master_beard",P)
