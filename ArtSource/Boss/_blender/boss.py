import sys, json, random; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/"; INFO={}
def fin(part,objs,sharp=38):
    o=join(f"boss_{part}",objs,sharp); export(o,OUT+f"Boss/boss_{part}.fbx")
    INFO[part]=info(o); print("PART",part,INFO[part]); return o
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
Stone=mat("Stone","#77736e"); StoneD=mat("StoneDark","#4b4846"); Metal=mat("Metal","#3d3f45"); Cloth=mat("Cloth","#3e2622")
Bone=mat("Bone","#5f5d60"); Crystal=mat("Crystal","#cf7c2b"); Magic=mat("Magic","#d9632a"); Leather=mat("Leather","#4a3128")

# ---------------- HEAD: broken guardian crown-helm over the pack skull ----------------
P=[]
DC=Vector((0,0.03,1.99)); DS=(0.5,0.53,0.33)
def dome_fn(v):
    if v.z<0: v.z*=0.55
    return v
P.append(rbox("dome",Metal,(DS[0]*2,DS[1]*2,DS[2]*2),DC,cuts=3,round=0.95,fn=dome_fn))
band=cone("band",Stone,r1=0.53,r2=0.55,depth=0.15,loc=(0,0.03,1.78),segs=10,scale=(1,1.04,1)); P.append(band)
P.append(rbox("nape",StoneD,(0.98,0.42,0.5),(0,0.33,1.55),cuts=2,round=0.6))
P.append(box("nasal",Stone,(0.09,0.08,0.3),(0,-0.52,1.7),bevel=0.02,rot=(4,0,0)))
# intact cheek + cracked visor bars on the right (-x); the left (+x) side is broken away
P.append(box("cheek",StoneD,(0.1,0.42,0.46),(-0.5,-0.14,1.52),bevel=0.03,taper=(1,1.25)))
P.append(box("visor",Stone,(0.26,0.08,0.09),(-0.36,-0.44,1.47),bevel=0.015,rot=(0,6,14)))
P.append(box("stub",StoneD,(0.1,0.2,0.22),(0.5,0.1,1.6),bevel=0.03,rot=(0,0,12),taper=(1,0.5)))
# crown points (stone), missing on the corrupted side
for (x,y,ln,wd) in ((0,-0.5,0.42,0.3),(-0.36,-0.4,0.32,0.26),(-0.54,0.0,0.36,0.28),(-0.38,0.44,0.3,0.26),(0.02,0.58,0.34,0.28)):
    n=Vector((x,y-0.03,0)).normalized()
    P.append(lock("crown",Stone,(x,y,1.88),(n.x*0.22,n.y*0.22,1),n,wd,ln,thick=0.45,r2=0.2))
# crystal horns growing out of the broken left side
for b,d,r,L in (((0.34,0.04,2.06),(0.7,-0.05,1),0.15,0.9),((0.44,0.24,1.92),(1,0.5,0.5),0.11,0.56),((0.44,-0.2,1.94),(1,-0.45,0.45),0.09,0.4)):
    P+=crystal(Crystal,b,d,r,L)
def dsurf(x,y):
    q=1-(x/DS[0])**2-((y-DC.y)/DS[1])**2
    return (x,y,DC.z+DS[2]*math.sqrt(max(q,0.02))*0.98+0.004)
P+=zig(Magic,[dsurf(*p) for p in ((0.3,0.0),(0.16,-0.12),(0.06,0.02),(-0.1,-0.1),(-0.2,0.06),(-0.33,-0.02))],w=0.035,h=0.03)
P+=zig(Magic,[dsurf(*p) for p in ((0.06,0.02),(0.02,0.2),(-0.08,0.3))],w=0.03,h=0.03)
fin("head",P)

# ---------------- SHOULDERS ----------------
P=[]
# left (+x): big cracked stone pauldron in two layers
P.append(rbox("pl1",Stone,(0.74,0.78,0.36),(0.52,0.0,1.38),rot=(0,18,0),cuts=2,round=0.55))
P.append(rbox("pl2",StoneD,(0.6,0.7,0.24),(0.76,0.0,1.2),rot=(0,42,0),cuts=2,round=0.5))
P.append(box("plrim",StoneD,(0.12,0.82,0.3),(0.2,0.0,1.42),bevel=0.035,rot=(0,10,0)))
P.append(lock("spk",StoneD,(0.62,0.0,1.5),(0.55,0,1),(0,-1,0),0.26,0.36,thick=0.7,r2=0.1))
Mq=Matrix.Translation((0.52,0,1.38))@Euler((0,math.radians(18),0)).to_matrix().to_4x4()
zz=[Mq@Vector(p) for p in ((-0.2,-0.3,0.19),(-0.08,-0.16,0.195),(-0.16,0.0,0.195),(0.0,0.1,0.195),(-0.08,0.26,0.19))]
P+=zig(Magic,zz,w=0.04,h=0.03,up=(0.3,0,1))
zz=[Mq@Vector(p) for p in ((-0.08,-0.16,0.195),(0.1,-0.2,0.19),(0.2,-0.1,0.175))]
P+=zig(Magic,zz,w=0.035,h=0.03,up=(0.3,0,1))
# right (-x): broken stone base overtaken by a crystal cluster
P.append(rbox("pr1",StoneD,(0.6,0.66,0.3),(-0.48,0.0,1.34),rot=(0,-18,0),cuts=2,round=0.5))
P.append(rbox("pr2",Stone,(0.4,0.5,0.2),(-0.7,0.02,1.2),rot=(0,-45,0),cuts=2,round=0.5))
for b,d,r,L in (((-0.52,0.02,1.4),(-0.6,0.05,1),0.16,0.9),((-0.64,-0.16,1.36),(-1,-0.45,0.6),0.12,0.58),((-0.6,0.2,1.38),(-0.75,0.7,0.7),0.12,0.6),((-0.76,0.0,1.28),(-1,0,0.2),0.1,0.42)):
    P+=crystal(Crystal,b,d,r,L)
fin("shoulders",P)

# ---------------- BACK: torn short cape + crystal fan ----------------
P=[]
P.append(rbox("mantle",Cloth,(0.98,0.34,0.24),(0,0.3,1.24),cuts=2,round=0.6))
for i,(x,ln,wd) in enumerate(((-0.42,0.62,0.3),(-0.22,0.86,0.32),(0.0,0.7,0.32),(0.21,0.92,0.32),(0.42,0.56,0.3))):
    y=0.36+0.07*(1-abs(x)/0.42)
    n=Vector((x*0.6,1,0.25)).normalized()
    P.append(lock("tatter",Cloth,(x,y,1.22),(x*0.42,0.24,-1),n,wd,ln,thick=0.16,r2=0.18))
P.append(rbox("root",StoneD,(0.56,0.26,0.4),(0.03,0.42,1.12),cuts=2,round=0.5))
for a,L,r in ((-46,0.7,0.12),(-12,1.15,0.16),(22,0.95,0.14),(56,0.62,0.11)):
    t=math.radians(a); d=(math.sin(t),0.5,math.cos(t))
    P+=crystal(Crystal,(0.03+0.16*math.sin(t),0.44,1.12+0.08*math.cos(t)),d,r,L)
fin("back",P)

# ---------------- WEAPON: two-handed cracked stone greatsword with an ember vein ----------------
hx,hz=-0.883,1.049; P=[]
P.append(tube("grip",Leather,[(hx,0.4,hz),(hx,-0.2,hz)],[0.05,0.05],sides=6))
P+=crystal(Crystal,(hx,0.38,hz),(0,1,0),0.085,0.26)
P.append(box("guard",StoneD,(0.78,0.16,0.18),(hx,-0.26,hz),bevel=0.04))
for s in (1,-1): P.append(box("gcap",Metal,(0.1,0.22,0.24),(hx+s*0.4,-0.26,hz),bevel=0.03))
P.append(box("vein",Magic,(0.1,1.98,0.05),(hx,-1.3,hz)))   # ember core seen through the gaps and along the fuller
segs=[(-0.36,-0.98,0.44,0.0,2),(-1.03,-1.6,0.4,0.025,-2.5),(-1.66,-2.1,0.35,-0.02,2)]
for y0,y1,wd,ox,rz in segs:
    L=y0-y1; yc=(y0+y1)/2
    for s in (1,-1):   # two halves leave the fuller open so the vein shows
        P.append(box("blade",Stone,(wd/2-0.012,L,0.12),(hx+ox+s*(wd/4+0.012),yc,hz),bevel=0.03,rot=(0,0,rz)))
        P.append(box("edge",StoneD,(0.05,L*0.96,0.05),(hx+ox+s*(wd/2+0.005),yc,hz),bevel=0.015,rot=(0,0,rz)))
tip=cone("tip",Stone,r1=0.2,r2=0.0,depth=0.34,segs=4,scale=(1,0.3,1))
tip.data.transform(Matrix.Translation((hx-0.02,-2.12,hz))@Euler((math.radians(90),0,0)).to_matrix().to_4x4()@Matrix.Rotation(math.pi/4,4,'Z')); P.append(tip)
for b,d,r,L in (((hx+0.2,-0.42,hz),(1,-0.5,0.5),0.07,0.3),((hx-0.2,-1.02,hz),(-1,-0.3,0.4),0.05,0.2)):
    P+=crystal(Crystal,b,d,r,L)
fin("weapon",P,sharp=30)

# ---------------- SHIELD: broken tower shield (flat in XY above the left hand, like the pack shields) ----------------
sx,sz=0.883,1.2; P=[]
P.append(box("slab",Stone,(0.96,0.95,0.11),(sx,0.3,sz),bevel=0.035))
P.append(box("slab2",Stone,(0.62,0.5,0.11),(sx-0.17,-0.4,sz),bevel=0.035,rot=(0,0,-6)))
P.append(box("slab3",Stone,(0.3,0.36,0.11),(sx-0.3,-0.78,sz),bevel=0.03,rot=(0,0,10)))
P.append(box("chunk",StoneD,(0.26,0.3,0.1),(sx+0.3,-0.3,sz-0.005),bevel=0.03,rot=(0,0,-24)))
for s in (1,-1): P.append(box("rim",Metal,(0.09,0.99,0.15),(sx+s*0.47,0.3,sz),bevel=0.025))
P.append(box("rimtop",Metal,(1.03,0.09,0.15),(sx,0.79,sz),bevel=0.025))
P.append(box("rimL",Metal,(0.09,0.6,0.15),(sx-0.49,-0.42,sz),bevel=0.025))
P.append(cone("boss",Metal,r1=0.2,r2=0.12,depth=0.1,loc=(sx,0.3,sz+0.05),segs=8))
P.append(box("bar",StoneD,(0.8,0.1,0.06),(sx,0.3,sz+0.045),bevel=0.015))
P+=zig(Magic,[(sx+0.16,-0.18,sz+0.056),(sx+0.02,-0.02,sz+0.056),(sx+0.14,0.1,sz+0.056),(sx+0.0,0.2,sz+0.056)],w=0.04,h=0.03)
P+=zig(Magic,[(sx+0.02,-0.02,sz+0.056),(sx-0.2,-0.06,sz+0.056),(sx-0.26,-0.3,sz+0.056)],w=0.035,h=0.03)
for b,d,r,L in (((sx+0.2,-0.2,sz),(0.6,-1,0.35),0.1,0.5),((sx+0.36,-0.12,sz),(1,-0.6,0.2),0.075,0.32)):
    P+=crystal(Crystal,b,d,r,L)
P.append(box("strap",Leather,(0.1,0.4,0.06),(sx,0.0,sz-0.08),bevel=0.02))
fin("shield",P,sharp=30)

# ---------------- CORE: ember heart inside the cracked ribcage ----------------
P=[ico("core",Magic,(0.42,0.36,0.44),(0.05,-0.08,1.02),sub=1)]
for b,d,r,L in (((0.12,-0.14,1.05),(0.45,-1,0.3),0.075,0.4),((0.0,-0.14,0.97),(-0.45,-1,-0.1),0.065,0.32),((0.08,-0.08,1.14),(0.15,-0.7,1),0.055,0.26)):
    P+=crystal(Magic,b,d,r,L)
fin("core",P,sharp=25)

# ---------------- TEXTURE ----------------
dst=OUT+"Boss/boss_body_texture.png"
recolor(R+"src/skeleton_texture.png",dst,{(1,2):("#3f3d42","#807d80"),(3,3):"#6d6a67",(4,3):"#3a3839",(6,3):"#5a372a",(7,3):"#322b29",(2,1):"#3e2622",(7,0):"#e08a2c",(6,1):"#4b4744",(2,3):"#141214"})
im=bpy.data.images.load(dst); w,h=im.size; px=np.array(im.pixels[:],dtype=np.float32).reshape(h,w,4)
rng=random.Random(7); cw,ch=w//8,h//4
warm=np.array([0.62,0.3,0.12],dtype=np.float32); hot=np.array([0.68,0.36,0.15],dtype=np.float32); dark=np.array([0.1,0.09,0.09],dtype=np.float32)
yy,xx=np.mgrid[0:h,0:w]
for (c,r),nb,nc in (((1,2),6,5),((3,3),5,4)):
    x0,y0=c*cw,r*ch
    inside=(xx>=x0)&(xx<x0+cw)&(yy>=y0)&(yy<y0+ch)
    for _ in range(nb):   # soft warm stains
        bx,by,br=x0+rng.uniform(10,cw-10),y0+rng.uniform(10,ch-10),rng.uniform(10,20)
        a=np.clip(1-np.hypot(xx-bx,yy-by)/br,0,1)**1.5*0.42*inside
        px[...,:3]=px[...,:3]*(1-a[...,None])+warm*a[...,None]
    for _ in range(nc):   # cracks: dark line with an ember core
        p=np.array([x0+rng.uniform(8,cw-8),y0+rng.uniform(8,ch-8)]); ang=rng.uniform(0,6.28)
        for k in range(rng.randint(3,5)):
            ang+=rng.uniform(-1.1,1.1); q=p+np.array([math.cos(ang),math.sin(ang)])*rng.uniform(9,16)
            ab=q-p; t=np.clip(((xx-p[0])*ab[0]+(yy-p[1])*ab[1])/(ab@ab),0,1)
            dist=np.hypot(xx-(p[0]+t*ab[0]),yy-(p[1]+t*ab[1]))
            m1=(dist<2.0)&inside; m2=(dist<0.8)&inside
            px[m1,:3]=dark; px[m2,:3]=hot
            p=q
out=bpy.data.images.new("o2",w,h,alpha=True); out.pixels=px.ravel(); out.filepath_raw=dst; out.file_format='PNG'; out.save()
json.dump(INFO,open(OUT+"Boss/info.json","w"),indent=1)
