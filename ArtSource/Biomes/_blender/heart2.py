# Coracao das Cinzas, segunda leva: altar partido com fenda acesa e respiradouro de brasa. Mesmo estilo de props.py.
# blender -b -P heart2.py   (lib.py em /home/souzx/quint_heroes)
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Biomes/Heart/"
def fin(name,objs):
    o=join(name,objs,30); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
def rock(name,m,size,loc,seed=0,rot=(0,0,0),sub=1,j=0.22):
    rng=random.Random(seed); return ico(name,m,size,loc,rot=rot,sub=sub,fn=lambda v: v*(1+rng.uniform(-j,j)))
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
def zig(m,pts,w=0.05,h=0.04,up=(0,0,1)):
    out=[]
    for a,b in zip(pts[:-1],pts[1:]):
        a=Vector(a);b=Vector(b);dd=(b-a).normalized(); out.append(tube("zig",m,[a-dd*w*0.4,b+dd*w*0.4],[(w/2,h/2)]*2,sides=4,up=up))
    return out
reset()
St=mat("Stone","#57524f"); SD=mat("StoneDark","#3a3635"); Glow=mat("Glow","#d9632a"); Cr=mat("Crystal","#cf7c2b")
# altar partido: dois blocos de apoio, laje rachada em duas metades, uma inclinada; fenda acesa entre elas
P=[box("legA",SD,(0.45,0.7,0.55),(-0.6,0,0.275),bevel=0.04), box("legB",SD,(0.45,0.7,0.48),(0.62,0.05,0.24),rot=(0,0,6),bevel=0.04),
   box("slabA",St,(0.95,0.95,0.18),(-0.42,0,0.64),rot=(0,-4,2),bevel=0.04), box("slabB",St,(0.9,0.9,0.18),(0.5,0.05,0.56),rot=(0,14,-5),bevel=0.04),
   box("glowcore",Glow,(0.08,0.8,0.3),(0.06,0.02,0.45))]
P+=zig(Glow,[(-0.75,-0.3,0.735),(-0.55,-0.1,0.735),(-0.68,0.15,0.735),(-0.5,0.32,0.735)],w=0.05,h=0.025)
P+=[rock("r",St,(0.3,0.26,0.2),(1.0,-0.5,0.09),seed=11), rock("r",SD,(0.24,0.2,0.16),(-1.05,0.45,0.07),seed=12)]
P+=crystal(Cr,(0.95,0.5,0.0),(0.4,0.2,1),0.09,0.55)+crystal(Cr,(-0.9,-0.55,0.0),(-0.3,-0.4,1),0.08,0.45)
fin("heart_altar",P)
reset()
St=mat("Stone","#57524f"); SD=mat("StoneDark","#3a3635"); Glow=mat("Glow","#d9632a"); Cr=mat("Crystal","#cf7c2b")
# respiradouro: anel de pedras escuras em volta de um poco aceso, com lascas de cristal na borda
P=[ico("pit",Glow,(1.0,1.0,0.12),(0,0,0.02),sub=2)]
rg=random.Random(7)
for k in range(9):
    a=2*math.pi*k/9+rg.uniform(-0.1,0.1); d=0.62
    P.append(rock("ring",SD if k%2 else St,(rg.uniform(0.34,0.44),rg.uniform(0.28,0.36),rg.uniform(0.24,0.42)),(d*math.cos(a),d*math.sin(a),0.12),seed=20+k,rot=(0,0,math.degrees(a))))
for k in range(3):
    a=2*math.pi*k/3+0.5
    P+=crystal(Cr,(0.78*math.cos(a),0.78*math.sin(a),0.1),(math.cos(a)*0.5,math.sin(a)*0.5,1),0.08,0.5+0.15*k)
fin("heart_vent",P)
