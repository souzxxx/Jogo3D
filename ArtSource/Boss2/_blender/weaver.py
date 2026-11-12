import sys, json, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Boss2/"; INFO={}; COL={}
os.makedirs(OUT,exist_ok=True)
def lathe(name,m,prof,segs=8,loc=(0,0,0),cap_top=True,rot=(0,0,0),scale=1.0):
    bm=bmesh.new(); rings=[]
    for r,z in prof: rings.append([bm.verts.new((r*math.cos(2*math.pi*k/segs),r*math.sin(2*math.pi*k/segs),z)) for k in range(segs)])
    for a,b in zip(rings[:-1],rings[1:]):
        for k in range(segs): bm.faces.new((a[k],a[(k+1)%segs],b[(k+1)%segs],b[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_top: bm.faces.new(rings[-1])
    o=new_obj(name,bm,m); return xf(o,loc,rot,(scale,)*3)
# measure the pack skull so the crown/veil sit just outside it
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=R+"src/Skeleton_Mage.fbx")
sk=bpy.data.objects["Skeleton_Mage_Skull"]; SV=[sk.matrix_world@v.co for v in sk.data.vertices]
def skull_r(z0,z1): 
    s=[math.hypot(v.x,v.y) for v in SV if z0<=v.z<z1]; return max(s) if s else 0.3
RC=skull_r(1.9,2.02); print("skull radius at crown",RC, "at 1.7",skull_r(1.65,1.75))
reset()
_mat=mat
def mat(n,hx,r=0.8): COL[n]=hx; return _mat(n,hx,r)
def fin(part,objs,sharp=38):
    o=join(f"weaver_{part}",objs,sharp); export(o,OUT+f"weaver_{part}.fbx"); INFO[part]=info(o); print("PART",part,INFO[part]); return o
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
def shard(m,c,d,r,L):
    d=Vector(d).normalized(); c=Vector(c)
    return [cone_dir("sh",m,c,d,r,L*0.72,segs=5), cone_dir("shb",m,c,-d,r,L*0.28,segs=5)]
Bone=mat("Bone","#a59d90"); BoneD=mat("BoneDark","#6f6a64"); StoneD=mat("StoneDark","#4b4846"); Stone=mat("Stone","#77736e")
Cloth=mat("Cloth","#4a2338"); ClothD=mat("ClothDark","#2e1824"); Crystal=mat("Crystal","#cf7c2b"); Magic=mat("Magic","#d9632a")

# ================= HEAD: tall crown + ember halo + torn veil =================
P=[]
ZB=1.97; RB=RC+0.035
P.append(lathe("band",BoneD,[(RB,ZB-0.07),(RB+0.03,ZB-0.06),(RB+0.03,ZB+0.06),(RB,ZB+0.07)],12,cap_top=False))
# spikes on the front 220 deg arc, tallest at the front
for i in range(9):
    a=math.radians(-90-110+i*27.5)          # -90 = front (-y)
    rel=abs(i-4)/4.0
    L=0.95-0.55*rel; W=0.2-0.06*rel
    o=Vector((math.cos(a),math.sin(a),0))
    base=Vector((RB*o.x,RB*o.y,ZB+0.03))
    m=Bone if i%2==0 else StoneD
    P.append(lock("spike",m,base,o*0.18+Vector((0,0,1)),o,W,L,thick=0.45,r2=0.12))
    if i%2==1: P+=crystal(Crystal,base+o*0.03+Vector((0,0,0.05)),o*0.4+Vector((0,0,1)),0.055,0.32)
# ember halo behind the head, tilted back
HC=Vector((0,0.42,2.28)); HR=0.66; tilt=math.radians(28)
Mh=Matrix.Translation(HC)@Matrix.Rotation(math.pi/2-tilt,4,'X')
ring=[Mh@Vector((HR*math.cos(2*math.pi*k/24),HR*math.sin(2*math.pi*k/24),0)) for k in range(25)]
P.append(tube("halo",Magic,ring,[(0.03,0.03)]*25,sides=5,cap_tip=False))
for k in range(8):
    t=2*math.pi*k/8+math.pi/8; o=Mh.to_3x3()@Vector((math.cos(t),math.sin(t),0))
    P.append(lock("ray",Bone,Mh@Vector((HR*math.cos(t),HR*math.sin(t),0)),o,Mh.to_3x3()@Vector((0,0,1)),0.1,0.2 if k%2 else 0.3,thick=0.4,r2=0.1))
for s in (1,-1):   # two thin struts tie the halo to the crown band
    P.append(tube("strut",BoneD,[(s*RB*0.7,RB*0.7,ZB),(s*0.3,0.5,2.05),(s*HR*0.62,HC.y+0.05,HC.z-0.3)],[0.025,0.022,0.02],sides=4))
# torn veil: strips from the back of the band hanging behind the head to the shoulders
rng=random.Random(5)
for i in range(7):
    a=math.radians(28+i*20.7)   # back arc only (28..152 deg), sides stay open
    o=Vector((math.cos(a),math.sin(a),0))
    end=1.0+0.18*abs(i-3)/3+rng.uniform(-0.06,0.08)
    pts=[(RB*o.x,RB*o.y,ZB+0.02),((RB+0.12)*o.x,(RB+0.12)*o.y,1.78),(0.58*o.x,0.58*o.y,1.45),(0.6*o.x,0.6*o.y+0.04,end+0.2),(0.6*o.x,0.6*o.y+0.07,end)]
    P.append(tube("veil",Cloth if i%2 else ClothD,pts,[(0.15,0.02),(0.17,0.022),(0.17,0.022),(0.14,0.018),(0.012,0.006)],sides=4,up=tuple(o)))
fin("head",P,sharp=40)

# ================= MANTLE: high collar behind the neck, lapels, torn back cape (chest) =================
P=[]
for i in range(8):
    a=math.radians(18+i*20.6)   # back arc only, so raised arms never sweep through it
    o=Vector((math.cos(a),math.sin(a),0))
    h=0.4+0.1*math.sin(math.radians(i*25.7))+rng.uniform(-0.04,0.04)
    P.append(lock("collar",Cloth if i%2 else ClothD,Vector((0.4*o.x,0.4*o.y+0.03,1.2)),o*0.5+Vector((0,0,1)),o,0.27,h,thick=0.16,r2=0.55))
for s in (1,-1):
    P.append(lock("lapel",ClothD,(s*0.15,-0.26,1.26),(s*0.1,-0.12,-1),(0,-1,0.1),0.15,0.38,thick=0.16,r2=0.4))
for i,(x,ln) in enumerate(((-0.3,0.78),(-0.15,0.95),(0.0,0.86),(0.15,0.98),(0.3,0.74))):
    P.append(tube("cape",Cloth if i%2 else ClothD,[(x,0.33,1.26),(x*1.1,0.42,1.0),(x*1.15,0.46,1.26-ln*0.75),(x*1.15,0.48,1.26-ln)],[(0.11,0.018),(0.12,0.02),(0.1,0.018),(0.01,0.006)],sides=4,up=(0,1,0)))
fin("mantle",P,sharp=40)
# ================= SHOULDERS: cloth drape + small stone plates, one per upper arm =================
for s,side in ((1,"l"),(-1,"r")):
    P=[rbox("drape",Cloth,(0.26,0.46,0.13),(s*0.33,0.0,1.26),rot=(0,s*16,0),cuts=2,round=0.7),
       box("plate",Stone,(0.2,0.3,0.07),(s*0.35,0.0,1.34),rot=(0,s*20,0),bevel=0.025),
       box("plate2",StoneD,(0.14,0.24,0.06),(s*0.46,0.0,1.27),rot=(0,s*40,0),bevel=0.02),
       lock("tip",StoneD,(s*0.36,0.0,1.38),(s*0.5,0.1,1),(0,-1,0),0.1,0.14,thick=0.6,r2=0.1)]
    fin("shoulder_"+side,P,sharp=40)
# ================= CORE: ember heart + 3 threads down to the waist =================
SURF={1.1:-0.297,1.0:-0.29,0.9:-0.316,0.8:-0.371,0.7:-0.385,0.6:-0.385,0.55:-0.38}
def fy(z):
    ks=sorted(SURF); 
    for a,b in zip(ks,ks[1:]):
        if a<=z<=b: t=(z-a)/(b-a); return SURF[a]+(SURF[b]-SURF[a])*t
    return SURF[ks[0]] if z<ks[0] else SURF[ks[-1]]
P=[ico("core",Magic,(0.28,0.22,0.28),(0,-0.29,1.06),sub=2), lathe("rim",BoneD,[(0.15,-0.02),(0.18,0.0),(0.18,0.035),(0.14,0.04)],8,loc=(0,-0.27,1.06),rot=(90,0,0),cap_top=False)]
for k,(x1,x2) in enumerate(((-0.08,-0.2),(0.0,0.03),(0.08,0.19))):
    pts=[]
    for j in range(7):
        z=1.0-j*0.075; x=x1+(x2-x1)*j/6+0.025*math.sin(j*1.9+k)
        pts.append((x,fy(z)-0.02,z))
    P.append(tube("thread",Magic,pts,[0.022]+[0.017]*5+[0.006],sides=4))
fin("core",P,sharp=30)

# ================= STAFF: long bone staff, ember orb held by claws (along -y like the pack staffs) =================
hx,hz=-0.883,1.049; P=[]
P.append(tube("shaft",Bone,[(hx,0.95,hz),(hx,-1.72,hz)],[0.045,0.04],sides=6))
for y in (0.85,0.45,-0.45,-0.9,-1.3):
    P.append(lathe("knuckle",BoneD,[(0.06,-0.04),(0.075,0.0),(0.06,0.04)],6,loc=(hx,y,hz),rot=(90,0,0),cap_top=False))
P.append(box("grip",ClothD,(0.11,0.34,0.11),(hx,0.0,hz),bevel=0.025))
P+=crystal(Crystal,(hx,0.95,hz),(0,1,0),0.06,0.22)
P.append(lathe("cup",StoneD,[(0.05,0.0),(0.14,0.06),(0.16,0.12),(0.12,0.15)],6,loc=(hx,-1.7,hz),rot=(90,0,0)))
OC=Vector((hx,-2.0,hz)); P.append(ico("orb",Magic,(0.34,0.34,0.34),OC,sub=2))
for k in range(4):
    t=2*math.pi*k/4+math.pi/4; o=Vector((math.cos(t),0,math.sin(t)))
    pts=[(hx+0.12*o.x,-1.82,hz+0.12*o.z),(hx+0.24*o.x,-1.98,hz+0.24*o.z),(hx+0.2*o.x,-2.18,hz+0.2*o.z),(hx+0.07*o.x,-2.27,hz+0.07*o.z)]
    P.append(tube("claw",Bone,pts,[0.045,0.04,0.028,0.006],sides=5))
for b,d in (((hx+0.1,-1.7,hz+0.1),(0.6,-0.3,1)),((hx-0.1,-1.72,hz-0.08),(-0.7,-0.3,-0.6))): P+=crystal(Crystal,b,d,0.045,0.22)
fin("staff",P,sharp=30)

# ================= SHARDS: floating crystal shards around shoulders/back =================
P=[]
for c,d,r,L in (((0.86,0.3,1.55),(0.45,0.2,1),0.1,0.7),((-0.86,0.3,1.55),(-0.45,0.2,1),0.1,0.7),((0.6,0.62,1.05),(0.5,0.6,0.7),0.08,0.55),((-0.6,0.62,1.05),(-0.5,0.6,0.7),0.08,0.55),((0.0,0.82,1.55),(0,0.35,1),0.11,0.8),((0.95,0.1,2.1),(0.35,0,1),0.07,0.45)):
    P+=shard(Crystal,c,d,r,L)
fin("shards",P,sharp=30)

# ================= TEXTURE =================
dst=OUT+"weaver_body_texture.png"
recolor(R+"src/skeleton_texture.png",dst,{(1,2):("#3f3d42","#807d80"),(2,1):("#2c1626","#5a2a44"),(7,3):"#2f2a2c",(5,1):"#4e3a48",(1,1):"#3a2a48",(3,3):"#4b4846",(0,1):"#8a7f78",(7,0):"#e08a2c",(7,2):"#c8672f"})
im=bpy.data.images.load(dst); w,h=im.size; px=np.array(im.pixels[:],dtype=np.float32).reshape(h,w,4)
rng=random.Random(11); cw,ch=w//8,h//4
warm=np.array([0.55,0.27,0.12],dtype=np.float32); hot=np.array([0.68,0.36,0.15],dtype=np.float32); dark=np.array([0.1,0.09,0.09],dtype=np.float32)
yy,xx=np.mgrid[0:h,0:w]
for (c,r),nb,nc in (((1,2),6,5),):
    x0,y0=c*cw,r*ch; inside=(xx>=x0)&(xx<x0+cw)&(yy>=y0)&(yy<y0+ch)
    for _ in range(nb):
        bx,by,br=x0+rng.uniform(10,cw-10),y0+rng.uniform(10,ch-10),rng.uniform(10,20)
        a=np.clip(1-np.hypot(xx-bx,yy-by)/br,0,1)**1.5*0.42*inside; px[...,:3]=px[...,:3]*(1-a[...,None])+warm*a[...,None]
    for _ in range(nc):
        p=np.array([x0+rng.uniform(8,cw-8),y0+rng.uniform(8,ch-8)]); ang=rng.uniform(0,6.28)
        for k in range(rng.randint(3,5)):
            ang+=rng.uniform(-1.1,1.1); q=p+np.array([math.cos(ang),math.sin(ang)])*rng.uniform(9,16); ab=q-p
            t=np.clip(((xx-p[0])*ab[0]+(yy-p[1])*ab[1])/(ab@ab),0,1); dist=np.hypot(xx-(p[0]+t*ab[0]),yy-(p[1]+t*ab[1]))
            px[(dist<2.0)&inside,:3]=dark; px[(dist<0.8)&inside,:3]=hot; p=q
out=bpy.data.images.new("o2",w,h,alpha=True); out.pixels=px.ravel(); out.filepath_raw=dst; out.file_format='PNG'; out.save()
json.dump({"info":INFO,"colors":COL},open(OUT+"info.json","w"),indent=1)
