import sys, json, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Biomes/"; INFO={}; COL={}
_mat=mat
def mat(name,hx,rough=0.8):
    COL.setdefault(CUR[0],{})[name]=hx; return _mat(name,hx,rough)
CUR=[""]
def fin(name,objs,sharp=38):
    o=join(name,objs,sharp); export(o,OUT+f"{CUR[0]}/{name}.fbx"); INFO.setdefault(CUR[0],{})[name]=info(o); print("PART",CUR[0],name,INFO[CUR[0]][name]["tris"],INFO[CUR[0]][name]["min"],INFO[CUR[0]][name]["max"]); return o
def lathe(name,m,prof,segs=8,loc=(0,0,0),cap_top=True,rot=(0,0,0),scale=1.0):
    bm=bmesh.new(); rings=[]
    for r,z in prof: rings.append([bm.verts.new((r*math.cos(2*math.pi*k/segs),r*math.sin(2*math.pi*k/segs),z)) for k in range(segs)])
    for a,b in zip(rings[:-1],rings[1:]):
        for k in range(segs): bm.faces.new((a[k],a[(k+1)%segs],b[(k+1)%segs],b[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_top: bm.faces.new(rings[-1])
    o=new_obj(name,bm,m); return xf(o,loc,rot,(scale,)*3)
def rock(name,m,size,loc,seed=0,rot=(0,0,0),sub=1,j=0.22):
    rng=random.Random(seed)
    def fn(v):
        k=1+rng.uniform(-j,j); return v*k
    return ico(name,m,size,loc,rot=rot,sub=sub,fn=fn)
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
def zig(m,pts,w=0.05,h=0.04,up=(0,0,1)):
    out=[]
    for a,b in zip(pts[:-1],pts[1:]):
        a=Vector(a);b=Vector(b);dd=(b-a).normalized(); out.append(tube("zig",m,[a-dd*w*0.4,b+dd*w*0.4],[(w/2,h/2)]*2,sides=4,up=up))
    return out
def dome_fn(v):
    if v.z<0: v.z*=0.25
    return v
def patch(m,size,loc,rot=(0,0,0),seed=1): return rock("moss",m,size,loc,seed=seed,rot=rot,j=0.12)

# ======================= CRYPT =======================
CUR[0]="Crypt"; reset()
St=mat("Stone","#6c7c8a"); SD=mat("StoneDark","#485663"); SL=mat("StoneLight","#8c9aa5"); Moss=mat("Moss","#3d5947"); Water=mat("Water","#2d5d67"); Sh=mat("Shadow","#1b2127"); Urn=mat("Urn","#566d78"); UB=mat("UrnBand","#39454f")
P=[box("plinth",SD,(2.0,1.05,0.2),(0,0,0.1),bevel=0.05), box("body",St,(1.8,0.86,0.62),(0,0,0.5),bevel=0.06,taper=(1.04,1.06)),
   box("in",Sh,(1.6,0.66,0.04),(0,0,0.805)), box("lid",SL,(1.94,0.98,0.16),(0.2,-0.36,0.88),rot=(9,0,17),bevel=0.05), box("ridge",St,(1.5,0.5,0.1),(0.2,-0.38,0.99),rot=(9,0,17),bevel=0.04),
   box("cr1",SD,(0.9,0.1,0.05),(0.2,-0.39,1.05),rot=(9,0,17),bevel=0.01), box("cr2",SD,(0.1,0.34,0.05),(0.03,-0.44,1.045),rot=(9,0,17),bevel=0.01)]
for sx in (-1,1):
    for sy in (-1,1): P.append(box("post",SD,(0.16,0.16,0.7),(sx*0.86,sy*0.4,0.52),bevel=0.035))
P+=[patch(Moss,(0.5,0.3,0.1),(0.7,-0.1,1.02),rot=(9,0,17)), patch(Moss,(0.5,0.12,0.35),(-0.5,-0.45,0.4),seed=3), patch(Moss,(0.5,0.4,0.08),(-0.8,-0.5,0.03),seed=4)]
fin("crypt_sarcophagus",P)
def urn(c,s,broken=False,seed=0):
    prof=[(0.14,0),(0.24,0.1),(0.33,0.36),(0.33,0.5),(0.22,0.78),(0.15,0.86),(0.2,0.95)]
    o=[]
    if not broken:
        o.append(lathe("urn",Urn,prof,8,c,scale=s)); o.append(lathe("band",UB,[(0.335,0.38),(0.345,0.4),(0.345,0.48),(0.335,0.5)],8,c,scale=s)); o.append(lathe("lip",UB,[(0.16,0.9),(0.215,0.93),(0.21,0.97),(0.12,0.975)],8,c,scale=s))
    else:
        o.append(lathe("urn",Urn,prof[:4],8,c,scale=s,cap_top=False)); o.append(lathe("ins",Sh,[(0.0,0.3),(0.31,0.46)],8,c,scale=s,cap_top=False))
        rng=random.Random(seed)
        for k in range(8):
            a=2*math.pi*k/8+0.39; h=rng.uniform(0.06,0.3)
            o.append(lock("jag",Urn,(c[0]+0.325*s*math.cos(a),c[1]+0.325*s*math.sin(a),c[2]+0.49*s),(0.1*math.cos(a),0.1*math.sin(a),1),(math.cos(a),math.sin(a),0),0.25*s,h*s,thick=0.12,r2=0.1))
        for k in range(4):
            a=rng.uniform(0,6.28); d=rng.uniform(0.45,0.7)*s
            o.append(box("shard",Urn,(0.2*s,0.14*s,0.035*s),(c[0]+d*math.cos(a),c[1]+d*math.sin(a),0.03),rot=(rng.uniform(-12,12),rng.uniform(-12,12),rng.uniform(0,180)),taper=(0.4,0.7)))
    return o
P=urn((-0.5,0.2,0),1.5)+urn((0.45,0.3,0),1.2)+urn((0.1,-0.5,0),1.25,broken=True,seed=5)+[patch(Moss,(0.5,0.4,0.07),(-0.1,0.1,0.03),seed=2)]
fin("crypt_urns",P)
P=[box("b1",SD,(1.1,1.1,0.3),(0,0,0.15),bevel=0.06), box("b2",St,(0.86,0.86,0.25),(0,0,0.42),bevel=0.05),
   lathe("robe",St,[(0.42,0.54),(0.36,1.0),(0.3,1.45),(0.34,1.6)],8), rbox("shoulders",St,(0.86,0.5,0.4),(0,0.02,1.66),cuts=2,round=0.6), box("break",SD,(0.5,0.34,0.06),(0,0.02,1.87),rot=(0,14,0))]
P.append(box("stubn",St,(0.3,0.3,0.2),(-0.12,0.04,1.9),rot=(0,22,10),bevel=0.05,taper=(0.5,0.6)))
for s in (1,-1):
    P.append(tube("arm",SL,[(s*0.38,0.0,1.6),(s*0.36,-0.22,1.3),(s*0.1,-0.36,1.2)],[0.13,0.12,0.1],sides=6))
P+=[box("hilt",SD,(0.5,0.08,0.08),(0,-0.4,1.2),bevel=0.02), box("sword",SL,(0.12,0.07,0.75),(0,-0.4,0.85),bevel=0.02), box("pom",SD,(0.1,0.1,0.14),(0,-0.4,1.3),bevel=0.02)]
P+=[rbox("head",St,(0.5,0.5,0.5),(0.78,-0.55,0.24),rot=(60,20,30),cuts=2,round=0.8), cone_dir("hoodtip",St,(0.86,-0.42,0.36),(0.6,0.8,0.5),0.2,0.3,segs=5), box("face",Sh,(0.26,0.05,0.28),(0.7,-0.74,0.2),rot=(60,20,30)),
    rock("rub",SD,(0.3,0.26,0.2),(-0.7,-0.6,0.09),seed=2), rock("rub",St,(0.2,0.2,0.16),(-0.5,-0.72,0.07),seed=3), patch(Moss,(0.5,0.3,0.1),(-0.2,0.2,0.57),seed=6), patch(Moss,(0.4,0.14,0.3),(0.3,0.26,1.3),seed=7)]
fin("crypt_statue",P)
P=[]; rng=random.Random(3)
for k in range(10):
    a=2*math.pi*k/10; h=0.22+rng.uniform(-0.04,0.06)
    if k==7: h=0.1
    P.append(box("blk",St if k%2 else SL,(0.56,0.3,h),(0.9*math.cos(a),0.9*math.sin(a),h/2),rot=(0,0,math.degrees(a)+90),bevel=0.04))
P+=[lathe("floor",SD,[(0.95,0.0),(0.95,0.05)],12), lathe("water",Water,[(0.86,0.05),(0.86,0.13)],12), rock("r",SD,(0.3,0.24,0.2),(1.05,-0.75,0.08),seed=1), patch(Moss,(0.5,0.3,0.08),(-0.9,0.3,0.24),seed=2), patch(Moss,(0.4,0.3,0.06),(0.3,0.95,0.23),seed=3),
    rock("isl",SL,(0.3,0.26,0.16),(0.25,-0.2,0.13),seed=5)]
fin("crypt_pool",P)
P=[]
for (x,y,r,h,sd) in ((0,0,0.36,2.0,1),(0.55,0.3,0.26,1.3,2),(-0.5,0.25,0.24,1.0,3),(-0.2,-0.5,0.2,0.7,4),(0.45,-0.4,0.17,0.5,5),(-0.75,-0.25,0.14,0.4,6)):
    rg=random.Random(sd); lx,ly=rg.uniform(-0.08,0.08),rg.uniform(-0.08,0.08)
    r*=1.2; P.append(tube("stal",SD,[(x,y,0),(x+lx*0.5,y+ly*0.5,h*0.45)],[r,r*0.7],sides=6,cap_tip=False)); P.append(tube("stal2",St,[(x+lx*0.5,y+ly*0.5,h*0.45),(x+lx,y+ly,h*0.8),(x+lx*1.3,y+ly*1.3,h)],[r*0.7,r*0.4,0.04],sides=6))
P+=[lathe("puddle",Water,[(0.6,0.0),(0.55,0.03)],9,loc=(0.5,-0.55,0)), lathe("puddle",Water,[(0.4,0.0),(0.36,0.03)],8,loc=(-0.6,0.5,0))]
fin("crypt_stalagmites",P)

# ======================= GARDEN =======================
CUR[0]="Garden"; reset()
St=mat("Stone","#a7a492"); SD=mat("StoneDark","#7b796b"); Bark=mat("Bark","#8f8877"); BarkD=mat("BarkDark","#6c6658"); Moss=mat("Moss","#5d7d44"); MossD=mat("MossDark","#466238"); Fern=mat("Fern","#5a9150"); Glow=mat("Glow","#cde6ae"); Soil=mat("Soil","#4d3f34")
P=[]
trunk=bez((0,0,0),(0.25,0.1,0.6),(-0.3,-0.1,1.1),(0.05,0.0,1.6),6)
P.append(tube("trunk",Bark,trunk,[0.42,0.36,0.33,0.31,0.3,0.28,0.26],sides=7,cap_tip=True))
for (p1,p2,p3,r0) in (((0.5,0.1,1.9),(0.9,0.2,2.0),(1.15,0.1,2.5),0.2),((-0.5,0.2,1.9),(-0.95,0.3,1.8),(-1.2,0.1,2.25),0.19),((0.0,-0.4,2.0),(-0.1,-0.8,2.2),(0.2,-0.9,2.6),0.16),((0.1,0.4,2.0),(0.3,0.7,2.3),(0.0,0.9,2.45),0.14)):
    pts=bez((0.05,0,1.5),p1,p2,p3,5); P.append(tube("br",Bark,pts,[r0,r0*0.85,r0*0.7,r0*0.55,r0*0.4,0.03],sides=5))
    m=pts[3]; P.append(tube("tw",BarkD,[m,(m[0]*1.25,m[1]*1.2+0.1,m[2]+0.3)],[r0*0.4,0.02],sides=4))
for a in (20,140,255): 
    t=math.radians(a); P.append(tube("root",BarkD,[(0.2*math.cos(t),0.2*math.sin(t),0.35),(0.6*math.cos(t),0.6*math.sin(t),0.12),(0.95*math.cos(t),0.95*math.sin(t),0.0)],[0.2,0.15,0.06],sides=5))
P+=[patch(Moss,(0.5,0.4,0.16),(0.62,0.15,2.1),seed=1), patch(Moss,(0.4,0.3,0.14),(-0.85,0.28,1.95),seed=2), patch(MossD,(0.5,0.3,0.3),(0.12,-0.25,0.9),seed=3), rock("knot",BarkD,(0.22,0.14,0.22),(-0.12,-0.3,1.15),seed=4)]
fin("garden_tree",P,sharp=42)
P=[]
for (x,y,h,rc,sd) in ((0,0.1,1.45,0.62,1),(0.75,-0.25,0.95,0.45,2),(-0.65,-0.3,0.7,0.38,3),(0.2,-0.75,0.4,0.26,4),(-0.45,0.6,0.5,0.3,5)):
    rg=random.Random(sd); lx=rg.uniform(-0.12,0.12)
    P.append(tube("stem",St,[(x,y,0),(x+lx*0.5,y,h*0.5),(x+lx,y,h)],[rc*0.3,rc*0.22,rc*0.2],sides=6))
    P.append(rbox("cap",SD,(rc*2,rc*2,rc*1.1),(x+lx,y,h+rc*0.07),rot=(rg.uniform(-10,10),rg.uniform(-10,10),0),cuts=2,round=0.95,fn=dome_fn))
    P.append(lathe("gills",Glow,[(rc*0.22,-0.1*rc),(rc*0.9,-0.02*rc),(rc*0.9,0.0)],8,loc=(x+lx,y,h),cap_top=False))
    P.append(patch(Moss,(rc*0.8,rc*0.6,rc*0.25),(x+lx+rc*0.2,y+rc*0.1,h+rc*0.5),seed=sd))
fin("garden_mushrooms",P,sharp=42)
P=[]
for (a,b,c,d,r) in (((-1.0,-0.2,-0.1),(-0.8,0.0,1.3),(0.5,0.2,1.5),(0.95,0.1,-0.1),0.22),((-0.6,0.5,-0.1),(-0.3,0.7,0.9),(0.4,0.3,0.8),(0.5,-0.5,-0.1),0.17),((0.2,-0.7,-0.1),(0.0,-0.5,0.7),(-0.6,-0.6,0.6),(-0.85,-0.75,-0.1),0.14)):
    pts=bez(a,b,c,d,7); P.append(tube("root",Bark,pts,[r,r*0.95,r*0.85,r*0.8,r*0.8,r*0.85,r*0.9,r*0.75],sides=6))
    P.append(tube("rootlet",BarkD,[pts[5],(pts[5][0]+0.3,pts[5][1]-0.3,0.1),(pts[5][0]+0.5,pts[5][1]-0.45,-0.05)],[r*0.5,r*0.35,0.03],sides=4))
P+=[patch(Moss,(0.6,0.4,0.14),(-0.15,0.1,1.2),seed=1), patch(MossD,(0.4,0.3,0.1),(0.1,0.42,0.7),seed=2), rock("st",St,(0.4,0.34,0.26),(0.75,0.6,0.1),seed=3), rock("st",SD,(0.26,0.22,0.18),(-0.4,-0.2,0.07),seed=4)]
fin("garden_roots",P,sharp=42)
P=[box("base",SD,(1.9,1.0,0.12),(0,0,0.06),bevel=0.03), box("wf",St,(1.9,0.16,0.5),(0,-0.42,0.3),bevel=0.04), box("wb",St,(1.9,0.16,0.5),(0,0.42,0.3),bevel=0.04), box("wl",St,(0.16,0.7,0.5),(-0.87,0,0.3),bevel=0.04),
   box("wr",St,(0.16,0.5,0.24),(0.87,0.1,0.17),bevel=0.04,rot=(0,0,8)), box("soil",Soil,(1.6,0.7,0.1),(0,0,0.42)), rock("rb",St,(0.3,0.24,0.2),(1.2,-0.3,0.08),seed=1), rock("rb",SD,(0.22,0.2,0.16),(1.05,0.35,0.06),seed=2),
   box("cap",SD,(0.5,0.22,0.08),(-0.6,-0.42,0.58),bevel=0.02), patch(Moss,(0.6,0.12,0.3),(0.3,-0.5,0.3),seed=5)]
rg=random.Random(4)
for (cx,cy,n,L) in ((-0.5,0.0,6,0.55),(0.15,0.08,7,0.65),(0.65,-0.05,5,0.45)):
    for k in range(n):
        a=2*math.pi*k/n+rg.uniform(-0.3,0.3)
        P.append(lock("frond",Fern,(cx,cy,0.45),(math.cos(a),math.sin(a),0.9),(-math.cos(a),-math.sin(a),1),0.2,L*rg.uniform(0.8,1.1),thick=0.14,r2=0.1))
fin("garden_planter",P)
P=[box("base",SD,(2.0,1.1,0.2),(0,0,0.1),bevel=0.05), rbox("body",St,(1.35,0.66,0.6),(0.1,0,0.5),cuts=2,round=0.75), rbox("haunch",St,(0.6,0.74,0.56),(0.55,0,0.5),cuts=2,round=0.8),
   tube("neck",St,[(-0.4,0,0.6),(-0.62,0,1.0),(-0.7,0,1.3)],[0.24,0.19,0.17],sides=6), rbox("head",St,(0.5,0.36,0.34),(-0.86,0,1.4),rot=(0,-12,0),cuts=2,round=0.8), rbox("snout",St,(0.3,0.22,0.2),(-1.1,0,1.32),rot=(0,-12,0),cuts=2,round=0.7),
   box("legf",St,(0.6,0.16,0.16),(-0.6,-0.24,0.28),bevel=0.04,rot=(0,0,-8)), box("legf2",St,(0.5,0.16,0.16),(-0.5,0.26,0.28),bevel=0.04,rot=(0,0,10)), box("legb",St,(0.5,0.16,0.18),(0.5,-0.38,0.29),bevel=0.04), cone_dir("tail",St,(0.8,0,0.62),(1,0,0.2),0.1,0.2,segs=5)]
for s in (1,-1): P.append(lock("ear",St,(-0.72,s*0.16,1.5),(0.3,s*1,0.5),(0,0,1),0.16,0.26,thick=0.4,r2=0.1))
P+=[tube("antler",SD,[(-0.74,0.1,1.56),(-0.6,0.26,1.9),(-0.36,0.34,2.15)],[0.08,0.065,0.03],sides=5), tube("tine",SD,[(-0.6,0.26,1.9),(-0.82,0.38,2.1)],[0.06,0.025],sides=5), tube("stub",SD,[(-0.74,-0.1,1.56),(-0.66,-0.2,1.76)],[0.06,0.05],sides=5),
    rbox("mossb",Moss,(1.2,0.6,0.3),(0.2,0.02,0.72),cuts=2,round=0.8), patch(MossD,(0.5,0.4,0.14),(0.55,-0.1,0.8),seed=1), patch(Moss,(0.3,0.26,0.1),(-0.84,0.02,1.58),seed=2), patch(MossD,(0.6,0.3,0.08),(-0.5,0.4,0.22),seed=3), box("antf",SD,(0.4,0.07,0.07),(-0.9,-0.42,0.24),rot=(0,0,30))]
fin("garden_statue",P,sharp=42)

# ======================= ASH =======================
CUR[0]="Ash"; reset()
Iron=mat("Iron","#3e4046"); IronL=mat("IronLight","#5c5f66"); Coal=mat("Charcoal","#252120"); Glow=mat("Glow","#d9632a"); Ash=mat("Ash","#77736f"); Wood=mat("Wood","#765240"); WoodL=mat("WoodLight","#9a7558"); Burnt=mat("Burnt","#332823")
P=[]
for k in range(3):
    a=math.radians(90+k*120); P.append(tube("leg",Iron,[(0.5*math.cos(a),0.5*math.sin(a),0.0),(0.26*math.cos(a),0.26*math.sin(a),0.6),(0.38*math.cos(a),0.38*math.sin(a),0.95)],[(0.05,0.05)]*3,sides=4)); P.append(box("foot",IronL,(0.16,0.16,0.06),(0.5*math.cos(a),0.5*math.sin(a),0.03),bevel=0.015))
P+=[lathe("ringlow",IronL,[(0.3,0.56),(0.3,0.64),(0.24,0.64),(0.24,0.56)],8), lathe("bowl",Iron,[(0.2,0.8),(0.46,0.88),(0.62,1.1),(0.62,1.2)],8), lathe("rim",IronL,[(0.6,1.16),(0.68,1.18),(0.68,1.25),(0.6,1.25)],8), lathe("ashb",Coal,[(0.6,1.2),(0.3,1.34)],8)]
rg=random.Random(2)
for k in range(9):
    a=2*math.pi*k/9; d=0.4 if k%2 else 0.24; m=Glow if k%2==0 else Coal
    P.append(rock("coal",m,(rg.uniform(0.24,0.32),)*3,(d*math.cos(a),d*math.sin(a),1.3+(0.4-d)*0.3),seed=k))
P+=[rock("emb",Glow,(0.34,0.34,0.26),(0,0,1.44),seed=30)]
fin("ash_brazier",P)
P=[rbox("ashpile",Ash,(2.0,1.6,0.24),(0,0,0.06),cuts=2,round=0.8)]
for (c,rot,L,m) in (((0,-0.3,0.2),(0,0,8),2.0,Burnt),((0.1,0.3,0.2),(0,0,-6),1.8,Coal),((-0.1,0.0,0.46),(0,6,74),1.7,Burnt),((0.3,0.05,0.5),(0,-22,110),1.9,Coal),((-0.35,0.1,0.78),(0,35,20),1.6,Burnt)):
    P.append(box("beam",m,(L,0.26,0.26),c,rot=rot,bevel=0.035)); 
    Mx=Matrix.Translation(c)@Euler([math.radians(a) for a in rot],'XYZ').to_matrix().to_4x4()
    P.append(lock("splint",m,Mx@Vector((L/2,0,0)),Mx.to_3x3()@Vector((1,0,0.15)),Mx.to_3x3()@Vector((0,0,1)),0.26,0.28,thick=0.8,r2=0.1,segs=4))
    P+=zig(Glow,[Mx@Vector(p) for p in ((-L*0.3,0.04,0.132),(-L*0.18,-0.05,0.132),(-L*0.05,0.03,0.132))],w=0.04,h=0.02,up=Mx.to_3x3()@Vector((0,0,1)))
P+=[rock("c",Coal,(0.26,0.22,0.18),(0.8,-0.6,0.12),seed=1), rock("c",Coal,(0.2,0.2,0.15),(-0.85,0.5,0.1),seed=2), rock("c",Glow,(0.14,0.14,0.1),(0.72,-0.45,0.13),seed=3)]
fin("ash_beams",P)
P=[lathe("stump",Wood,[(0.5,0),(0.44,0.12),(0.42,0.6)],9), lathe("top",WoodL,[(0.38,0.6),(0.38,0.615)],9), lathe("band",Iron,[(0.44,0.4),(0.445,0.4),(0.445,0.5),(0.435,0.5)],9),
   box("afoot",Iron,(0.6,0.36,0.12),(0,0,0.68),bevel=0.03), box("awaist",Iron,(0.34,0.24,0.2),(0,0,0.83),bevel=0.03), box("aface",IronL,(0.86,0.34,0.2),(0.02,0,1.02),bevel=0.035), cone_dir("horn",IronL,(-0.4,0,1.04),(-1,0,0.05),0.13,0.4,segs=6,r2=0.03), box("heel",Iron,(0.16,0.3,0.1),(0.46,0,0.98),bevel=0.02),
   tube("hh",Wood,[(0.1,-0.34,1.16),(0.25,0.3,1.16)],[0.035,0.035],sides=6)]
hb=box("hhead",Iron,(0.2,0.3,0.2),(0.09,-0.36,1.2),rot=(0,0,-13+90),bevel=0.03); P.append(hb)
for a in (40,160,280): t=math.radians(a); P.append(tube("rt",Wood,[(0.4*math.cos(t),0.4*math.sin(t),0.2),(0.62*math.cos(t),0.62*math.sin(t),0.0)],[0.13,0.06],sides=5))
P+=[box("tongs",IronL,(0.05,0.7,0.04),(0.56,0.1,0.33),rot=(52,0,12)), box("tongs",IronL,(0.05,0.7,0.04),(0.62,0.1,0.33),rot=(52,0,20))]
fin("ash_anvil",P)
P=[]; Rc=0.6; Lc=1.5
Mc=Matrix.Translation((0,0,Rc*0.96))@Euler((math.radians(8),math.radians(90),math.radians(15))).to_matrix().to_4x4()
def ring(z,r,w=0.05):
    o=[]
    for k in range(8):
        a0,a1=2*math.pi*k/8,2*math.pi*(k+1)/8
        o.append(tube("ring",IronL,[Mc@Vector((r*math.cos(a0),r*math.sin(a0),z)),Mc@Vector((r*math.cos(a1),r*math.sin(a1),z))],[(w,w)]*2,sides=4))
    return o
P+=ring(-Lc/2,Rc)+ring(0,Rc,0.035)+ring(Lc/2,Rc)
for k in range(8):
    a=2*math.pi*k/8
    if k==2: continue
    P.append(tube("bar",Iron,[Mc@Vector((Rc*math.cos(a),Rc*math.sin(a),-Lc/2)),Mc@Vector((Rc*math.cos(a),Rc*math.sin(a),Lc/2))],[(0.032,0.032)]*2,sides=4))
for z in (-Lc/2,Lc/2):
    for k in range(4):
        a=2*math.pi*k/8+0.39
        P.append(tube("cap",Iron,[Mc@Vector((Rc*math.cos(a),Rc*math.sin(a),z)),Mc@Vector((-Rc*math.cos(a),-Rc*math.sin(a),z))],[(0.03,0.03)]*2,sides=4))
P.append(tube("hook",IronL,[Mc@Vector((0,0,Lc/2)),Mc@Vector((0,0,Lc/2+0.2))],[(0.05,0.05)]*2,sides=4)); P+= [x for x in ring(Lc/2+0.28,0.1,0.03)]
# fallen door
for k in range(4): P.append(tube("door",Iron,[(-0.3+k*0.2,-1.0,0.03),(-0.4+k*0.2,-0.25,0.03)],[(0.03,0.03)]*2,sides=4))
P+=[tube("doorf",IronL,[(-0.38,-1.0,0.03),(0.38,-1.0,0.03)],[(0.04,0.04)]*2,sides=4), tube("doorf",IronL,[(-0.48,-0.25,0.03),(0.28,-0.25,0.03)],[(0.04,0.04)]*2,sides=4), tube("chain",IronL,[(1.0,0.3,0.03),(1.25,0.1,0.03),(1.3,-0.2,0.03)],[(0.045,0.03)]*3,sides=4)]
fin("ash_cage",P)

# ======================= HEART =======================
CUR[0]="Heart"; reset()
St=mat("Stone","#57524f"); SD=mat("StoneDark","#3a3635"); Glow=mat("Glow","#d9632a"); Cr=mat("Crystal","#cf7c2b")
P=[box("b1",SD,(1.5,1.5,0.25),(0,0,0.125),bevel=0.06), box("b2",St,(1.15,1.15,0.22),(0,0,0.36),bevel=0.05), box("core",Glow,(0.36,0.36,2.3),(0,0,1.6))]
segs=[(0.47,1.2,0.84,0.76,(0,0),0),(1.25,2.0,0.74,0.64,(0.04,-0.03),9),(2.06,2.62,0.62,0.52,(-0.03,0.03),-7)]
for si,(z0,z1,w0,w1,off,rz) in enumerate(segs):
    h=z1-z0; t=w1/w0
    P.append(box("seg",St,(w0,w0,h),(off[0],off[1],(z0+z1)/2),rot=(0,0,rz),bevel=0.05,taper=(t,t)))
    Mz=Matrix.Translation((off[0],off[1],0))@Matrix.Rotation(math.radians(rz),4,'Z')
    for fi,(ax,ay) in enumerate(((0,-1),(1,0),(0,1),(-1,0))):
        us=[0.1,-0.09,0.07,-0.06]
        if (fi+si)%2: us=[-u for u in us]
        pts=[]
        for k,u in enumerate(us):
            v=z0+0.08+(h-0.16)*k/3; hw=(w0+(w1-w0)*(v-z0)/h)/2+0.004
            pts.append(Mz@Vector((ax*hw-ay*u, ay*hw+ax*u, v)))
        nrm=Mz.to_3x3()@Vector((ax,ay,0))
        P+=zig(Glow,pts,w=0.07,h=0.03,up=nrm)
tipo=cone("tip",SD,r1=0.37,r2=0.0,depth=0.42,segs=4); tipo.data.transform(Matrix.Translation((-0.03,0.03,2.62))@Matrix.Rotation(math.radians(45-7),4,'Z')); P.append(tipo)
P+=[rock("rb",St,(0.4,0.34,0.28),(0.85,-0.7,0.12),seed=1), rock("rb",SD,(0.3,0.26,0.2),(-0.9,0.6,0.09),seed=2), rock("rb",St,(0.24,0.2,0.16),(-0.75,-0.8,0.07),seed=3)]
P+=crystal(Cr,(0.5,0.5,0.4),(0.5,0.5,1),0.1,0.6)+crystal(Cr,(-0.55,-0.4,0.4),(-0.6,-0.5,1),0.08,0.45)
fin("heart_spire",P,sharp=30)
P=[rock("base",SD,(1.7,1.5,0.6),(0,0,0.18),seed=4,sub=1), rock("base2",St,(0.8,0.7,0.4),(0.7,-0.5,0.14),seed=5), rock("base3",St,(0.6,0.6,0.36),(-0.75,0.4,0.12),seed=6)]
for b,d,r,L in (((0,0,0.2),(0.08,0.05,1),0.26,2.1),((0.3,0.1,0.2),(0.55,0.2,1),0.2,1.6),((-0.3,0.1,0.2),(-0.6,0.15,1),0.19,1.4),((0.0,-0.35,0.2),(0.1,-0.7,1),0.17,1.2),((-0.1,0.4,0.2),(-0.15,0.75,1),0.16,1.1),((0.55,-0.3,0.15),(1,-0.55,0.6),0.13,0.8),((-0.6,-0.3,0.15),(-1,-0.6,0.55),0.12,0.7),((0.5,0.5,0.15),(0.9,0.9,0.5),0.11,0.6)):
    P+=crystal(Cr,b,d,r,L)
fin("heart_crystals",P,sharp=30)
json.dump({"info":INFO,"colors":COL},open(OUT+"info.json","w"),indent=1)
