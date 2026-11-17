# Emblemas dos quatro elementos para os totens (titulo, Santuario e portas): substituem as esferas lisas.
# Low-poly facetado como o pack, origem no centro da base, ~0,7 m de altura. Rodar no Blender do PC:
#   blender -b -P emblems.py   (lib.py em /home/souzx/quint_heroes)
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
from mathutils import Vector
OUT=R+"out/Emblems/"
INFO={}
def fin(name,objs,sharp=30):
    o=join(name,objs,sharp); export(o,OUT+f"{name}.fbx"); INFO[name]=info(o); print("PART",name,INFO[name]); return o

def lathe(name,m,prof,segs=8,cap_top=True):
    bm=bmesh.new(); rings=[]
    for r,z in prof: rings.append([bm.verts.new((r*math.cos(2*math.pi*k/segs),r*math.sin(2*math.pi*k/segs),z)) for k in range(segs)])
    for a,b in zip(rings[:-1],rings[1:]):
        for k in range(segs): bm.faces.new((a[k],a[(k+1)%segs],b[(k+1)%segs],b[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_top: bm.faces.new(rings[-1])
    bmesh.ops.remove_doubles(bm,verts=bm.verts[:],dist=0.0005)
    return new_obj(name,bm,m)

reset()
# ---- Fogo: chama de corpo bojudo e ponta curvada, duas linguas menores e o miolo claro, sobre um carvao
flame=mat("Flame","#c8592a"); core=mat("FlameCore","#e9a44e"); coal=mat("Coal","#3b2c27")
def bend(o,k,ax=0):
    for v in o.data.vertices: v.co[ax]+=k*v.co.z*v.co.z
    o.data.update(); return o
body=[(0.0,0.0),(0.15,0.05),(0.2,0.16),(0.18,0.3),(0.12,0.45),(0.05,0.6),(0.0,0.74)]
parts=[ico("coal",coal,(0.36,0.36,0.12),(0,0,0.04),sub=1)]
parts.append(xf(bend(lathe("flame",flame,body,segs=6),0.18),(0,0,0.06)))
parts.append(xf(bend(lathe("core",core,[(0.0,0.0),(0.09,0.04),(0.11,0.12),(0.08,0.24),(0.03,0.34),(0.0,0.42)],segs=6),0.12),(0.02,-0.12,0.08)))
for x,y,sc,rz,k in [(0.17,0.05,0.55,25,-0.5),(-0.16,0.03,0.48,-30,0.5)]:
    t=lathe("tongue",flame,body,segs=5); bend(t,k)
    parts.append(xf(t,(x,y,0.06),(0,rz*0.6,0),(sc,sc,sc)))
fin("emblem_fire",parts)

reset()
# ---- Agua: gota facetada com duas gotinhas em volta
water=mat("Water","#3b78a8"); light=mat("WaterLight","#7fb3d6")
drop=lathe("drop",water,[(0.0,0.0),(0.17,0.05),(0.23,0.17),(0.21,0.3),(0.14,0.45),(0.06,0.58),(0.0,0.68)],segs=8)
small1=xf(lathe("d1",light,[(0.0,0.0),(0.06,0.02),(0.075,0.06),(0.05,0.1),(0.0,0.15)],segs=6),(0.3,0.05,0.18))
small2=xf(lathe("d2",light,[(0.0,0.0),(0.05,0.02),(0.06,0.05),(0.04,0.08),(0.0,0.12)],segs=6),(-0.26,-0.12,0.32))
fin("emblem_water",[drop,small1,small2])

reset()
# ---- Terra: pedra com tres pontas de cristal ambar
rockm=mat("Rock","#7d6240"); dark=mat("RockDark","#55432e"); amber=mat("Amber","#c49452")
rnd=random.Random(4)
def jit(v):
    return v*(1+rnd.uniform(-0.2,0.2))
parts=[ico("rock",rockm,(0.46,0.4,0.34),(0,0,0.17),sub=1,fn=jit), ico("rock2",dark,(0.22,0.2,0.18),(0.2,-0.12,0.08),sub=1,fn=jit)]
for base,d,r,L in [((0,0.02,0.28),(0.05,0.0,1),0.08,0.42),((-0.1,0.08,0.24),(-0.45,0.2,1),0.06,0.3),((0.1,0.06,0.24),(0.4,0.25,1),0.055,0.27)]:
    parts.append(cone_dir("cry",amber,base,d,0.7*r,0.7*L,segs=6,r2=r)); 
    tip=Vector(base)+Vector(d).normalized()*0.7*L
    parts.append(cone_dir("cryt",amber,tip,d,r,0.3*L,segs=6,r2=0.0))
fin("emblem_earth",parts)

reset()
# ---- Ar: redemoinho de tres faixas que estreitam para cima (cada uma um pouco deslocada), com a fita girando por dentro
wind=mat("Wind","#8fc7b2"); windl=mat("WindLight","#c3e3d4")
parts=[]
for z,r,dx,m in [(0.12,0.27,0.0,wind),(0.33,0.21,0.04,windl),(0.53,0.14,0.08,wind)]:
    ring=[(dx+r*math.cos(2*math.pi*k/12),r*math.sin(2*math.pi*k/12),z+0.025*math.sin(2*math.pi*k/12)) for k in range(13)]
    parts.append(tube("band",m,ring,[(0.055,0.018)]*13,sides=4,up=(0,0,1)))
pts=[]; radii=[]
for i in range(20):
    t=i/19; ang=t*math.pi*2*1.3
    rr=0.12*(1-t)+0.03
    pts.append((0.05*t+rr*math.cos(ang),rr*math.sin(ang),0.04+t*0.66)); radii.append((0.03*(1-0.5*t)+0.012,0.012))
parts.append(tube("swirl",windl,pts,radii,sides=4,up=(0,0,1)))
fin("emblem_air",parts)

import json; open(OUT+"info.json","w").write(json.dumps(INFO,indent=1))
