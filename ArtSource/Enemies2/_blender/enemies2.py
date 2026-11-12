import sys, json, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Enemies2/"; os.makedirs(OUT,exist_ok=True); INFO={}; COL={}
_mat=mat
def mat(n,hx,r=0.8): COL.setdefault(CUR[0],{})[n]=hx; return _mat(n,hx,r)
CUR=[""]
def fin(name,objs,sharp=38):
    o=join(name,objs,sharp); export(o,OUT+name+".fbx"); INFO[name]=info(o); print("PART",name,INFO[name]["tris"],INFO[name]["min"],INFO[name]["max"]); return o
def skull_r(fbx,obj,z0,z1):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=R+f"src/{fbx}.fbx")
    o=bpy.data.objects[obj]; vs=[o.matrix_world@v.co for v in o.data.vertices]
    s=[math.hypot(v.x,v.y) for v in vs if z0<=v.z<z1]; return max(s) if s else 0.4
def shell(name,m,size,loc,keep,thick=0.035,cuts=3,round=0.9,fn=None):
    o=rbox(name,m,size,loc,cuts=cuts,round=round,fn=fn)
    bm=bmesh.new(); bm.from_mesh(o.data)
    dead=[f for f in bm.faces if not keep(f.calc_center_median())]
    bmesh.ops.delete(bm,geom=dead,context='FACES')
    r=bmesh.ops.solidify(bm,geom=bm.faces[:],thickness=thick)
    bm.to_mesh(o.data); bm.free(); return o
def flat_tube(name,m,pts,rad,up): return tube(name,m,pts,rad,sides=4,up=up)
def flame(m,base,nrm,L,W,curl=(0,0.3,0),thick=0.06):
    base=Vector(base); nrm=Vector(nrm).normalized(); c=Vector(curl)
    pts=bez(base-nrm*0.04,base+nrm*L*0.35,base+nrm*L*0.75+c*L*0.3,base+nrm*L+c*L*0.6,5)
    side=nrm.cross(Vector((0,0,1)) if abs(nrm.z)<0.9 else Vector((1,0,0)))
    up=side.cross(nrm) if side.length>1e-3 else Vector((0,1,0))
    return tube("flame",m,pts,[(W*0.7,thick),(W,thick),(W*0.95,thick),(W*0.6,thick*0.8),(W*0.28,thick*0.6),(0.01,0.01)],sides=4,up=tuple(up))
def zig(m,pts,w=0.04,h=0.03,up=(0,0,1)):
    out=[]
    for a,b in zip(pts[:-1],pts[1:]):
        a=Vector(a);b=Vector(b);dd=(b-a).normalized(); out.append(tube("zig",m,[a-dd*w*0.4,b+dd*w*0.4],[(w/2,h/2)]*2,sides=4,up=up))
    return out
def rock(name,m,size,loc,seed=0,rot=(0,0,0),sub=1,j=0.2):
    rng=random.Random(seed)
    return ico(name,m,size,loc,rot=rot,sub=sub,fn=lambda v:v*(1+rng.uniform(-j,j)))
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
hx,hz=-0.883,1.049

# ======================= CINZENTO (Skeleton_Minion) =======================
CUR[0]="Cinzento"; RS=skull_r("Skeleton_Minion","Skeleton_Minion_Head",1.6,1.9); print("minion skull r",RS); reset()
Rag=mat("Rag","#6e6863"); RagD=mat("RagDark","#47423f"); EarIn=mat("RagInner","#8a6f66"); Bone=mat("Bone","#b5ab98"); Rope=mat("Rope","#7a6248")
P=[]
def hood_fn(v):
    v.y+= 0.12*max(0,v.z)    # crown leans back...
    return v
P.append(shell("hood",Rag,(1.06,1.08,1.0),(0,0.06,1.8),lambda c: not (c.y<-0.22 and c.z<2.06 and abs(c.x)<0.5),thick=0.04,fn=hood_fn))
P.append(lock("peak",RagD,(0,-0.36,2.08),(0,-1,-0.55),(0,-0.45,1),0.46,0.3,thick=0.25,r2=0.3))
for s in (1,-1):
    pts=bez((s*0.34,0.06,2.06),(s*0.5,0.08,2.26),(s*0.7,0.14,2.34),(s*0.84,0.22,2.22),5)
    P.append(flat_tube("ear",Rag,pts,[(0.1,0.03),(0.2,0.03),(0.25,0.03),(0.24,0.03),(0.16,0.025),(0.03,0.01)],up=(0.0,-1,0.1)))
    P.append(flat_tube("earin",EarIn,[(p[0],p[1]-0.03,p[2]) for p in pts[1:5]],[(0.1,0.02),(0.16,0.02),(0.15,0.02),(0.06,0.01)],up=(0,-1,0.1)))
for i,(x,ln) in enumerate(((-0.32,0.42),(-0.1,0.55),(0.12,0.48),(0.33,0.38))):
    P.append(lock("tatter",RagD if i%2 else Rag,(x,0.48,1.5),(x*0.3,0.35,-1),(0,1,0.2),0.24,ln,thick=0.15,r2=0.15))
fin("cinzento_head",P,sharp=40)
P=[rbox("hump",Rag,(0.86,0.6,0.62),(0.02,0.36,1.18),rot=(18,0,0),cuts=2,round=0.8), rbox("hump2",RagD,(0.6,0.46,0.42),(-0.12,0.5,1.36),rot=(25,0,10),cuts=2,round=0.8),
   rbox("patch",RagD,(0.36,0.3,0.3),(0.24,0.56,1.08),cuts=2,round=0.8)]
for b,d,L in (((0.2,0.6,1.4),(0.4,0.5,0.8),0.42),((-0.18,0.66,1.22),(-0.6,0.6,0.4),0.36),((0.05,0.7,1.0),(0.2,1,-0.2),0.3)):
    b=Vector(b); d=Vector(d).normalized(); P.append(tube("bone",Bone,[b,b+d*L],[0.035,0.03],sides=5)); P.append(ico("knob",Bone,(0.1,0.1,0.1),b+d*L,sub=1))
P.append(tube("rope",Rope,[(-0.38,-0.1,1.3),(-0.3,0.25,1.42),(0.1,0.52,1.5),(0.38,0.3,1.0)],[(0.03,0.03)]*4,sides=4))
P.append(tube("tail",RagD,bez((0,0.62,0.9),(0,0.95,0.6),(0.25,1.25,0.1),(0.45,1.5,0.02),6),[0.07,0.06,0.05,0.04,0.03,0.02,0.006],sides=5))
fin("cinzento_back",P,sharp=40)
P=[box("wrap",Rope,(0.14,0.22,0.14),(hx,0.0,hz),bevel=0.03)]
for x,zz in ((-0.05,0.04),(0.0,-0.01),(0.05,0.04)):
    P.append(tube("claw",Bone,[(hx+x,-0.08,hz+zz),(hx+x*1.4,-0.28,hz+zz+0.06),(hx+x*1.6,-0.45,hz+zz-0.02)],[0.03,0.022,0.004],sides=5))
fin("cinzento_weapon",P)

# ======================= BRASA VIVA (Skeleton_Warrior) =======================
CUR[0]="Brasa"; RW=skull_r("Skeleton_Warrior","Skeleton_Warrior_Head",1.9,2.02); print("warrior skull r",RW); reset()
Coal=mat("Charcoal","#2b2624"); CoalL=mat("CharcoalLight","#4a423d"); Magic=mat("Magic","#d9632a")
P=[]
ZB=1.96; RB=RW+0.03
P.append(cone("band",Coal,r1=RB+0.03,r2=RB,depth=0.14,loc=(0,0,ZB-0.07),segs=10))
for i in range(6):
    a=math.radians(90+30+i*60); o=Vector((math.cos(a),math.sin(a),0))
    L=0.42+0.1*(i%2)
    P.append(flame(Magic,Vector((RB*0.85*o.x,RB*0.85*o.y,ZB+0.02)),o*0.45+Vector((0,0,1)),L,0.34,curl=(0,0.4,0.25),thick=0.07))
P.append(flame(Magic,(0,0.0,2.08),(0,0.1,1),0.72,0.4,curl=(0,0.45,0.25),thick=0.08))
P.append(flame(Magic,(0,0.25,2.0),(0,0.7,0.8),0.55,0.36,curl=(0,0.3,0.3),thick=0.07))
for s in (1,-1): P.append(flame(Magic,(s*0.38,-0.05,1.62),(s*1,0.4,0.6),0.32,0.15,curl=(0,0.4,0.3)))
fin("brasa_head",P,sharp=40)
P=[rbox("chest",Coal,(0.66,0.2,0.46),(0,-0.31,1.02),rot=(-6,0,0),cuts=2,round=0.6), rbox("back",Coal,(0.66,0.2,0.46),(0,0.3,1.05),rot=(6,0,0),cuts=2,round=0.6),
   rbox("chunk",CoalL,(0.26,0.12,0.2),(0.18,-0.38,1.16),rot=(0,0,12),cuts=2,round=0.6)]
P+=zig(Magic,[(-0.22,-0.42,1.18),(-0.1,-0.425,1.05),(-0.02,-0.43,1.12),(0.08,-0.425,0.95),(0.2,-0.42,1.0)],w=0.045,h=0.03,up=(0,-1,0))
P+=zig(Magic,[(-0.02,-0.43,1.12),(0.02,-0.425,1.24)],w=0.04,h=0.03,up=(0,-1,0))
P+=zig(Magic,[(-0.2,0.405,1.2),(-0.05,0.41,1.05),(0.1,0.41,1.15),(0.22,0.405,0.98)],w=0.045,h=0.03,up=(0,1,0))
fin("brasa_body",P,sharp=40)
for s,side in ((1,"l"),(-1,"r")):
    P=[rbox("pad",Coal,(0.44,0.5,0.26),(s*0.42,0.0,1.27),rot=(0,s*18,0),cuts=2,round=0.6), rbox("pad2",CoalL,(0.3,0.4,0.18),(s*0.6,0.0,1.16),rot=(0,s*42,0),cuts=2,round=0.6)]
    Mq=Matrix.Translation((s*0.42,0,1.27))@Euler((0,math.radians(s*18),0)).to_matrix().to_4x4()
    P+=zig(Magic,[Mq@Vector(p) for p in ((-s*0.12,-0.2,0.13),(-s*0.02,-0.08,0.135),(-s*0.1,0.04,0.135),(s*0.04,0.16,0.13))],w=0.04,h=0.03,up=(s*0.3,0,1))
    P.append(flame(Magic,(s*0.46,0.02,1.38),(s*0.4,0.2,1),0.34,0.16,curl=(0,0.4,0.2)))
    fin("brasa_shoulder_"+side,P,sharp=40)
P=[tube("haft",Coal,[(hx,0.3,hz),(hx,-0.7,hz)],[0.05,0.055],sides=6), box("wrap",CoalL,(0.12,0.3,0.12),(hx,0.0,hz),bevel=0.025),
   rock("head",Coal,(0.52,0.56,0.5),(hx,-0.92,hz),seed=3,j=0.18), rock("head2",CoalL,(0.3,0.3,0.28),(hx+0.16,-1.08,hz+0.12),seed=4)]
P+=zig(Magic,[(hx-0.2,-0.8,hz+0.21),(hx-0.06,-0.92,hz+0.255),(hx+0.06,-0.84,hz+0.25),(hx+0.18,-1.0,hz+0.2)],w=0.05,h=0.03)
P+=zig(Magic,[(hx-0.25,-0.85,hz-0.05),(hx-0.27,-0.98,hz+0.06),(hx-0.25,-1.08,hz-0.04)],w=0.045,h=0.03,up=(-1,0,0))
for b,d in (((hx,-1.12,hz+0.18),(0,-0.6,1)),((hx+0.22,-0.9,hz+0.1),(1,-0.2,0.6))): P.append(flame(Magic,b,d,0.3,0.14,curl=(0,0.3,0.2)))
fin("brasa_weapon",P,sharp=40)

# ======================= SENTINELA (Skeleton_Mage) =======================
CUR[0]="Sentinela"; reset()
St=mat("Stone","#7d8274"); SD=mat("StoneDark","#545a50"); Moss=mat("Moss","#56713f"); MossD=mat("MossDark","#3f5532"); Magic=mat("Magic","#bfe3b0")
def mossp(size,loc,seed,rot=(0,0,0)): return rock("moss",Moss if seed%2 else MossD,size,loc,seed=seed,rot=rot,j=0.12)
P=[box("helm",St,(1.04,1.02,0.92),(0,0.0,1.74),bevel=0.08,taper=(0.92,0.92)), box("cap",SD,(1.1,1.08,0.14),(0,0.0,2.25),bevel=0.04),
   box("crest",St,(0.5,0.6,0.22),(0,0.02,2.42),bevel=0.05,taper=(0.7,0.8)), box("brow",SD,(0.9,0.12,0.12),(0,-0.5,1.93),bevel=0.03),
   box("slit_frame",SD,(0.74,0.08,0.18),(0,-0.5,1.78),bevel=0.02), box("eye",Magic,(0.62,0.06,0.07),(0,-0.53,1.78)),
   box("nose",St,(0.12,0.1,0.34),(0,-0.52,1.55),bevel=0.03), box("chin",SD,(0.96,0.94,0.12),(0,0,1.31),bevel=0.03),
   mossp((0.6,0.5,0.12),(0.15,0.1,2.52),1), mossp((0.4,0.3,0.1),(-0.38,0.3,2.33),2), mossp((0.3,0.14,0.36),(0.5,0.1,1.7),3)]
for s in (1,-1): P.append(box("ear",SD,(0.12,0.4,0.5),(s*0.52,0.05,1.7),bevel=0.03))
fin("sentinela_head",P,sharp=35)
def shell_fn(v):
    if v.y<0: v.y*=0.4
    return v
P=[rbox("shell",St,(1.5,0.95,1.2),(0,0.62,1.1),rot=(-12,0,0),cuts=3,round=0.85,fn=shell_fn), box("ridge",SD,(0.2,0.55,1.05),(0,1.02,1.15),rot=(-12,0,0),bevel=0.05),
   box("rim",SD,(1.5,0.45,0.14),(0,0.55,0.5),bevel=0.04)]
for s in (1,-1): P.append(box("plate",SD,(0.42,0.32,0.7),(s*0.52,0.9,1.12),rot=(-12,0,s*-22),bevel=0.05))
P+=[mossp((1.1,0.6,0.26),(0.05,0.82,1.66),1,rot=(-30,0,0)), mossp((0.6,0.45,0.22),(-0.48,0.72,1.55),2,rot=(-20,0,0)), mossp((0.5,0.36,0.2),(0.52,0.95,1.4),3,rot=(-50,0,0)),
    rock("stone",SD,(0.3,0.26,0.22),(0.36,1.02,1.62),seed=4)]
fin("sentinela_back",P,sharp=35)
P=[]
rng=random.Random(2)
for k in range(8):
    a=2*math.pi*k/8+math.pi/8; h=0.2+rng.uniform(-0.03,0.04)
    if k==5: h=0.1
    P.append(box("blk",St if k%2 else SD,(0.5,0.36,h),(0.5*math.cos(a),0.5*math.sin(a),h/2),rot=(0,0,math.degrees(a)+90),bevel=0.04))
P.append(box("core",SD,(0.62,0.62,0.16),(0,0,0.08),bevel=0.03,rot=(0,0,22.5)))
for k,a in enumerate((20,95,160,230,305)):
    t=math.radians(a); o=Vector((math.cos(t),math.sin(t),0))
    P.append(tube("root",St,[o*0.55+Vector((0,0,0.15)),o*0.85+Vector((0,0,0.1)),o*1.05+Vector((0,0,-0.02))],[0.12,0.09,0.04],sides=5))
P+=[mossp((0.5,0.3,0.1),(0.45,0.3,0.21),1), mossp((0.4,0.3,0.08),(-0.5,-0.2,0.2),2), mossp((0.3,0.3,0.08),(0.85,-0.35,0.06),3)]
fin("sentinela_base",P,sharp=35)
P=[tube("shaft",SD,[(hx,0.42,hz),(hx,-0.5,hz)],[0.05,0.05],sides=6), box("grip",MossD,(0.11,0.28,0.11),(hx,0.0,hz),bevel=0.02),
   box("obelisk",St,(0.2,0.55,0.2),(hx,-0.78,hz),bevel=0.03,rot=(90,0,0),taper=(0.75,0.75)), box("collar",SD,(0.26,0.1,0.26),(hx,-0.52,hz),bevel=0.03),
   box("pomm",St,(0.13,0.1,0.13),(hx,0.46,hz),bevel=0.03)]
P+=crystal(Magic,(hx,-1.04,hz),(0,-1,0),0.07,0.3)
P.append(mossp((0.16,0.2,0.08),(hx+0.02,-0.72,hz+0.1),5))
fin("sentinela_weapon",P,sharp=35)
json.dump({"info":INFO,"colors":COL},open(OUT+"info.json","w"),indent=1)
