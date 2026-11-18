# Pedestal do Santuario: octogono de pedra em degraus, fuste com quatro runas entalhadas, capitel com encaixe raso
# onde o emblema do elemento flutua. Altura 1,25 m (a mesma do topo usado pelo RunDirector). blender -b -P shrine.py
import sys, math, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Shrine/"
def fin(name,objs):
    o=join(name,objs,38); export(o,OUT+name+".fbx"); i=info(o); print("PART",name,i["tris"],i["min"],i["max"]); return o
def lathe(name,m,prof,segs=8,cap_top=True):
    bm=bmesh.new(); rings=[]
    for r,z in prof: rings.append([bm.verts.new((r*math.cos(2*math.pi*k/segs+math.pi/8),r*math.sin(2*math.pi*k/segs+math.pi/8),z)) for k in range(segs)])
    for a,b in zip(rings[:-1],rings[1:]):
        for k in range(segs): bm.faces.new((a[k],a[(k+1)%segs],b[(k+1)%segs],b[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_top: bm.faces.new(rings[-1])
    return new_obj(name,bm,m)
reset()
SD=mat("StoneDark","#5a5866"); St=mat("Stone","#77737f"); SL=mat("StoneLight","#9a95a0"); Rune=mat("Rune","#cdbb8a"); Sock=mat("Socket","#3a3842"); Moss=mat("Moss","#5d7d44")
P=[lathe("step1",SD,[(0.66,0),(0.66,0.12),(0.6,0.14)],8),
   lathe("step2",St,[(0.52,0.14),(0.52,0.26),(0.46,0.28)],8),
   lathe("shaft",St,[(0.36,0.28),(0.33,0.98)],8),
   lathe("neck",SD,[(0.38,0.98),(0.38,1.03)],8),
   lathe("cap",SL,[(0.36,1.03),(0.47,1.12),(0.47,1.2),(0.4,1.25)],8,cap_top=False),
   lathe("socket",Sock,[(0.4,1.25),(0.28,1.19),(0.0,1.18)],8,cap_top=False)]
# quatro runas: uma em cada face principal do fuste
for k in range(4):
    a=k*math.pi/2; r=0.345
    cx,cy=r*math.cos(a),r*math.sin(a)
    rot=(0,0,math.degrees(a)+90)
    P.append(box("rune",Rune,(0.06,0.02,0.34),(cx,cy,0.62),rot=rot))
    P.append(box("runeA",Rune,(0.16,0.02,0.05),(cx,cy,0.74),rot=rot))
    P.append(box("runeB",Rune,(0.12,0.02,0.05),(cx*1.0,cy*1.0,0.5),rot=(rot[0],rot[1]+0,rot[2]),bevel=0.0))
# musgo no degrau
rg=random.Random(4)
for k in range(3):
    a=rg.uniform(0,6.28)
    P.append(ico("moss",Moss,(0.22,0.16,0.05),(0.56*math.cos(a),0.56*math.sin(a),0.13),sub=1))
fin("shrine_pedestal",P)
