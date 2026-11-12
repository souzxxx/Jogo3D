import sys, json; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Boss/"; INFO={}
def fin(part,objs,sharp=38):
    o=join(f"boss_{part}",objs,sharp); export(o,OUT+f"boss_{part}.fbx"); INFO[part]=info(o); print("PART",part,INFO[part]); return o
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
def zig(m,pts,w=0.03,h=0.03,up=(0,0,1)):
    out=[]
    for a,b in zip(pts[:-1],pts[1:]):
        a=Vector(a);b=Vector(b);dd=(b-a).normalized()
        out.append(tube("zig",m,[a-dd*w*0.4,b+dd*w*0.4],[(w/2,h/2)]*2,sides=4,up=up))
    return out
reset()
Stone=mat("Stone","#77736e"); StoneD=mat("StoneDark","#4b4846"); Crystal=mat("Crystal","#cf7c2b"); Magic=mat("Magic","#d9632a")
# arm joint: upperarm.l head (0.212,0,1.107); arm radius ~0.11 (top z 1.217). Keep everything at |x| >= 0.2 so it swings with the arm.
# ---- LEFT (+x): big cracked stone pauldron, two layers, over the upper arm
P=[]
P.append(rbox("pl1",Stone,(0.62,0.74,0.32),(0.5,0.0,1.3),rot=(0,14,0),cuts=2,round=0.55))
P.append(rbox("pl2",StoneD,(0.46,0.66,0.22),(0.7,0.0,1.15),rot=(0,40,0),cuts=2,round=0.5))
P.append(box("plrim",StoneD,(0.1,0.78,0.26),(0.23,0.0,1.3),bevel=0.03,rot=(0,8,0)))
P.append(lock("spk",StoneD,(0.58,0.0,1.42),(0.6,0,1),(0,-1,0),0.24,0.32,thick=0.7,r2=0.1))
Mq=Matrix.Translation((0.5,0,1.3))@Euler((0,math.radians(14),0)).to_matrix().to_4x4()
P+=zig(Magic,[Mq@Vector(p) for p in ((-0.17,-0.28,0.165),(-0.06,-0.15,0.17),(-0.13,0.0,0.17),(0.02,0.1,0.17),(-0.05,0.25,0.165))],w=0.04,h=0.03,up=(0.25,0,1))
P+=zig(Magic,[Mq@Vector(p) for p in ((-0.06,-0.15,0.17),(0.1,-0.19,0.165),(0.19,-0.09,0.15))],w=0.035,h=0.03,up=(0.25,0,1))
fin("shoulder_l",P)
# ---- RIGHT (-x): broken stone base taken over by a crystal cluster; crystals point out/back, never inward
P=[]
P.append(rbox("pr1",StoneD,(0.52,0.62,0.28),(-0.47,0.0,1.27),rot=(0,-16,0),cuts=2,round=0.5))
P.append(rbox("pr2",Stone,(0.36,0.48,0.2),(-0.68,0.02,1.14),rot=(0,-42,0),cuts=2,round=0.5))
for b,d,r,L in (((-0.5,0.02,1.36),(-0.75,0.1,1),0.15,0.85),((-0.62,-0.16,1.32),(-1,-0.5,0.55),0.11,0.56),((-0.6,0.2,1.33),(-0.8,0.7,0.65),0.11,0.6),((-0.76,0.0,1.22),(-1,0,0.15),0.1,0.42)):
    P+=crystal(Crystal,b,d,r,L)
fin("shoulder_r",P)
json.dump(INFO,open(OUT+"info_shoulders.json","w"),indent=1)
