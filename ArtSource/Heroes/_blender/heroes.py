import sys, json; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/"
INFO={}
def fin(hero,part,objs,sharp=38):
    o=join(f"{hero.lower()}_{part}",objs,sharp)
    export(o,OUT+f"{hero}/{hero.lower()}_{part}.fbx")
    INFO.setdefault(hero,{})[part]=info(o); print("PART",hero,part,INFO[hero][part])
    return o
def neck(m,z0=1.12,z1=1.4,r=0.21):
    return cone("neck",m,r1=r,r2=r*0.95,depth=z1-z0,loc=(0,0.0,z0),segs=8)
def sym(fn):
    out=[]
    for s in (1,-1): out+=fn(s)
    return out

# =============================== RAX ===============================
def rax():
    reset()
    Fur=mat("Fur","#343a55"); FurD=mat("FurDark","#23273b"); FurL=mat("FurLight","#6d7790")
    Ear=mat("EarInner","#9c4a36"); Eye=mat("Eye","#17141f"); Nose=mat("Nose","#17141f")
    Magic=mat("Magic","#c8672f"); Claw=mat("Claw","#d6d2c6"); Leather=mat("Leather","#6e4433"); Metal=mat("Metal","#8d959c")
    P=[]
    C0=Vector((0,-0.02,1.68)); SX,SY,SZ=0.52,0.47,0.44
    def hf(v):
        v.x*=1+0.07*max(0,-v.z); return v
    P.append(rbox("head",Fur,(SX*2,SY*2,SZ*2),C0,cuts=4,round=1.0,fn=hf))
    P.append(neck(Fur))
    # short wide feline muzzle + chin
    P.append(rbox("muzzle",FurL,(0.5,0.26,0.24),(0,-0.43,1.5),cuts=2,round=0.95))
    P.append(rbox("chin",FurL,(0.3,0.2,0.16),(0,-0.4,1.38),cuts=2,round=0.95))
    P.append(cone_dir("nose",Nose,(0,-0.555,1.6),(0,0.15,-1),0.075,0.09,segs=3,flat=(1,0.6),r2=0.01))
    def side(s):
        o=[]
        o.append(rbox("cheek",Fur,(0.42,0.36,0.36),(s*0.33,-0.27,1.5),cuts=2,round=1.0))
        o.append(ico("eye",Eye,(0.13,0.06,0.2),(s*0.235,-0.425,1.76),rot=(0,0,s*-10),sub=2))
        o.append(box("brow",FurD,(0.26,0.09,0.065),(s*0.235,-0.4,1.9),rot=(-18,s*14,s*-10),bevel=0.02))
        d=Vector((s*0.3,0.0,1.0)).normalized(); eb=Vector((s*0.33,-0.08,1.9))
        o.append(cone_dir("ear",Fur,eb,d,0.24,0.44,segs=4,flat=(1,0.5),r2=0.06))
        o.append(cone_dir("earin",Ear,eb+Vector((s*0.005,-0.075,0.03)),d,0.15,0.3,segs=4,flat=(1,0.3),r2=0.03))
        o.append(cone_dir("tuft",FurD,eb+d*0.4,(s*0.5,0.15,1.0),0.075,0.13,segs=4,flat=(1,0.6),r2=0.02))
        o.append(lock("ruff",FurL,(s*0.42,-0.22,1.5),(s*1.0,0.1,-0.6),(0,-1,0),0.3,0.3,thick=0.5,r2=0.15))
        return o
    P+=sym(side)
    # ember mane: flat layered locks from the crown down the nape (crest), wider at the back
    rows=[(-6,0.0,0.36,0.3),(26,0.2,0.4,0.34),(58,0.3,0.44,0.38),(90,0.33,0.46,0.4),(120,0.26,0.44,0.42)]
    for th,xo,wd,ln in rows:
        t=math.radians(th)
        for sx in ((0,) if xo==0 else (-1,0,1)):
            x=sx*xo
            k=math.sqrt(max(0.05,1-(x/SX)**2))
            nrm=Vector((x/SX*0.9,math.sin(t)*k,math.cos(t)*k)).normalized()
            pos=C0+Vector((x,SY*math.sin(t)*k,SZ*math.cos(t)*k))-nrm*0.03
            tan=Vector((sx*0.35,math.cos(t),-math.sin(t))).normalized()
            d=(tan+nrm*0.3).normalized()
            P.append(lock("mane",Magic,pos,d,nrm,wd*(1.0 if sx==0 else 0.92),ln,thick=0.36,r2=0.42))
    fin("Rax","head",P,sharp=52)
    # tail
    pts=bez((0,0.24,0.62),(0,0.85,0.30),(0.05,1.30,0.62),(0.22,1.22,1.22),9)
    T=[tube("tail",Fur,pts,[0.085]*4+[0.078,0.074,0.07,0.068,0.066,0.066],sides=6,cap_tip=True)]
    end=Vector(pts[-1]); d=(end-Vector(pts[-2])).normalized()
    def tear(v):
        k=1-0.55*max(0,v.z); v.x*=k; v.y*=k; return v
    tip=ico("tip",Magic,(0.3,0.3,0.5),(0,0,0),sub=1,fn=tear)
    tip.data.transform(Matrix.Translation(end+d*0.17)@d.to_track_quat('Z','Y').to_matrix().to_4x4()); T.append(tip)
    T.append(cone_dir("tip2",Magic,end+d*0.05,d+Vector((0.5,0.3,0.1)),0.08,0.3,segs=4))
    fin("Rax","tail",T)
    # claws (left then mirrored)
    def claws(s):
        C=[]
        C.append(box("band",Leather,(0.2,0.34,0.32),(s*0.90,0,1.105),bevel=0.035))
        C.append(box("plate",Metal,(0.07,0.38,0.24),(s*1.0,0,1.115),bevel=0.02))
        for y in (-0.125,0.0,0.125):
            pts=[(s*0.98,y,1.14),(s*1.14,y,1.15),(s*1.28,y,1.12),(s*1.4,y,1.04)]
            C.append(tube("claw",Claw,pts,[(0.035,0.075),(0.033,0.07),(0.025,0.05),(0.003,0.006)],sides=4))
        return C
    fin("Rax","weapon_l",claws(1)); fin("Rax","weapon_r",claws(-1))
    recolor(R+"src/rogue_texture.png",OUT+"Rax/rax_body_texture.png",{
        (0,2):"#353b57",(1,2):"#a3472f",(5,1):"#272b41",(7,2):"#272b41",(3,1):"#3b4160",(5,3):"#8a5a3c",(6,3):"#5e3a30",(0,3):"#353b57",(1,3):"#272b41"})

# =============================== LIORA ===============================
def liora():
    reset()
    Fur=mat("Fur","#c4703f"); FurD=mat("FurDark","#7a3f30"); FurL=mat("FurLight","#e8d5b0")
    Ear=mat("EarInner","#d9907f"); Eye=mat("Eye","#231c24"); Nose=mat("Nose","#231c24")
    Antler=mat("Antler","#6b4a3b"); Blossom=mat("Blossom","#e0a09a"); Leaf=mat("Leaf","#5c8f4e"); Magic=mat("Magic","#4a9cc2")
    P=[]
    def hf(v):
        v.x*=1+0.02-0.10*max(0,-v.z)+0.06*max(0,0.4-abs(v.z)); return v
    P.append(rbox("head",Fur,(1.0,0.94,0.9),(0,-0.02,1.68),cuts=4,round=0.9,fn=hf))
    P.append(neck(Fur,r=0.19))
    def mz(v):
        t=max(0,-v.y); v.x*=1-0.35*t; v.z*=1-0.25*t; return v
    P.append(rbox("muzzle",FurL,(0.42,0.5,0.3),(0,-0.5,1.5),cuts=2,round=0.6,fn=mz))
    P.append(ico("nose",Nose,(0.13,0.1,0.1),(0,-0.755,1.555),sub=1))
    def side(s):
        o=[]
        o.append(ico("eye",Eye,(0.14,0.07,0.21),(s*0.26,-0.445,1.75),rot=(0,0,s*-8),sub=2))
        o.append(cone_dir("cheek",FurL,(s*0.38,-0.16,1.5),(s*1.0,0.15,-0.5),0.18,0.3,segs=4,flat=(1,0.55)))
        d=(s*1.0,0.12,0.55)
        o.append(cone_dir("ear",Fur,(s*0.36,0.0,1.9),d,0.22,0.5,segs=4,flat=(1,0.45),r2=0.03))
        o.append(cone_dir("earin",Ear,(s*0.4,-0.06,1.915),d,0.135,0.36,segs=4,flat=(1,0.3),r2=0.015))
        # antlers
        a0=(s*0.17,0.08,2.02); a1=(s*0.30,0.12,2.42); a2=(s*0.50,0.16,2.74); a3=(s*0.46,0.12,3.02)
        o.append(tube("antler",Antler,[a0,a1,a2,a3],[0.075,0.065,0.05,0.03],sides=5))
        t1=(s*0.08,0.0,2.74); t2=(s*0.82,0.14,2.92)
        o.append(tube("tine1",Antler,[a1,(s*0.2,0.05,2.6),t1],[0.055,0.045,0.028],sides=5))
        o.append(tube("tine2",Antler,[a2,(s*0.68,0.16,2.8),t2],[0.045,0.04,0.028],sides=5))
        for p in (a3,t2): o.append(ico("blossom",Blossom,(0.17,0.17,0.15),p,sub=1))
        o.append(ico("leaf",Leaf,(0.2,0.1,0.07),(t1[0]+s*0.02,t1[1],t1[2]+0.03),rot=(0,s*-25,20),sub=1))
        o.append(ico("leaf",Leaf,(0.2,0.1,0.07),(a2[0]+s*0.05,a2[1]+0.06,a2[2]+0.02),rot=(10,s*25,60),sub=1))
        return o
    P+=sym(side)
    # forehead blossom + back tuft
    P.append(ico("blossom",Blossom,(0.17,0.12,0.16),(0.24,-0.33,2.07),sub=1))
    P.append(cone_dir("tuft",Fur,(0,-0.12,2.06),(0,-0.5,0.7),0.17,0.18,segs=4,flat=(1,0.7),r2=0.03))
    fin("Liora","head",P,sharp=52)
    # tail: big bushy fox tail
    pts=bez((0,0.24,0.62),(0,0.8,0.45),(0,1.3,0.75),(0,1.12,1.5),10)
    rad=[0.1,0.17,0.24,0.29,0.32,0.33,0.32,0.29,0.24,0.16,0.02]
    T=[tube("tail",Fur,pts[:8],rad[:8],sides=8,cap_tip=True), tube("tailtip",FurL,pts[7:],rad[7:],sides=8)]
    fin("Liora","tail",T,sharp=45)
    # orb
    c=Vector((-0.9,-0.3,1.3))
    O=[ico("orb",Magic,(0.42,0.42,0.42),c,sub=2)]
    for k,a in enumerate((40,220)):
        r=0.31; p=c+Vector((math.cos(math.radians(a))*r,math.sin(math.radians(a))*r,(k-1)*0.09))
        def tear(v):
            kk=1-0.6*max(0,v.z); v.x*=kk; v.y*=kk; return v
        O.append(ico("drop",Magic,(0.09,0.09,0.13),p,sub=1,fn=tear))
    fin("Liora","weapon",O,sharp=25)
    recolor(R+"src/mage_texture.png",OUT+"Liora/liora_body_texture.png",{
        (0,2):"#4f7b4d",(4,3):"#d8c59c",(5,3):"#7a4a3a",(7,1):"#c4703f",(7,2):"#3f2c2b",(3,1):"#4a3030",(3,3):"#d8c59c",(2,2):"#3d6447",(1,2):"#4f7b4d"})

# =============================== BRANN ===============================
def brann():
    reset()
    Fur=mat("Fur","#7d8490"); FurD=mat("FurDark","#25252e"); FurW=mat("FurLight","#e2e4e3")
    Beard=mat("Beard","#b4b9c0"); Eye=mat("Eye","#d8b45a"); Nose=mat("Nose","#17141f"); Scar=mat("Scar","#c47a6e")
    Stone=mat("Stone","#8a8780"); StoneD=mat("StoneDark","#6d6a66"); Wood=mat("Wood","#6b4636"); Metal=mat("Metal","#565a62")
    Leather=mat("Leather","#8a5a3c"); Moss=mat("Moss","#5d7d49"); Magic=mat("Magic","#6f9a4e")
    P=[]
    def hf(v):
        v.x*=1+0.10*max(0,-v.z); v.z*=1-0.08*max(0,v.z); return v
    h=rbox("head",FurW,(1.12,0.98,0.86),(0,-0.02,1.66),cuts=6,round=0.88,fn=hf)
    def pf(c):
        ax=abs(c.x)
        if c.y>0.22 and c.z<1.98: return Fur
        if 0.13<ax<0.38 and c.z>1.52 and c.y<0.3: return FurD
        if c.y>0.22: return Fur
        return None
    paint(h,pf); P.append(h)
    P.append(neck(FurD,r=0.22))
    def mz(v):
        t=max(0,-v.y); v.x*=1-0.25*t; v.z*=1-0.15*t; return v
    P.append(rbox("muzzle",FurW,(0.48,0.46,0.32),(0,-0.5,1.5),cuts=2,round=0.6,fn=mz))
    P.append(ico("nose",Nose,(0.2,0.14,0.14),(0,-0.73,1.56),sub=1))
    def side(s):
        o=[]
        o.append(ico("eye",Eye,(0.09,0.05,0.1),(s*0.255,-0.475,1.74),sub=1))
        o.append(box("brow",Beard,(0.27,0.1,0.075),(s*0.255,-0.475,1.87),rot=(0,s*14,s*-8),bevel=0.025))
        o.append(ico("ear",FurD,(0.28,0.12,0.26),(s*0.47,0.06,2.0),rot=(0,s*20,0),sub=1))
        o.append(ico("earin",FurW,(0.15,0.06,0.14),(s*0.475,0.0,2.0),rot=(0,s*20,0),sub=1))
        o.append(cone_dir("whisk",Beard,(s*0.42,-0.22,1.48),(s*1.0,-0.05,-0.6),0.17,0.3,segs=4,flat=(1,0.6)))
        o.append(cone_dir("whisk",Beard,(s*0.3,-0.36,1.4),(s*0.5,-0.2,-1),0.16,0.3,segs=4,flat=(1,0.6)))
        return o
    P+=sym(side)
    P.append(cone_dir("beard",Beard,(0,-0.44,1.4),(0,-0.3,-1),0.2,0.42,segs=5,flat=(1,0.6)))
    P.append(cone_dir("beard",Beard,(0.14,-0.42,1.4),(0.2,-0.25,-1),0.15,0.3,segs=4,flat=(1,0.6)))
    P.append(cone_dir("beard",Beard,(-0.14,-0.42,1.4),(-0.2,-0.25,-1),0.15,0.3,segs=4,flat=(1,0.6)))
    P.append(box("scar",Scar,(0.04,0.04,0.34),(0.255,-0.485,1.78),rot=(-8,14,0),bevel=0.008))
    fin("Brann","head",P,sharp=52)
    # tail
    pts=[(0,0.26,0.62),(0,0.5,0.56),(0,0.72,0.6)]
    T=[tube("tail",Fur,pts,[0.13,0.16,0.13],sides=6), cone_dir("tailtip",FurD,(0,0.7,0.6),(0,1,0.35),0.13,0.24,segs=6)]
    fin("Brann","tail",T)
    # hammer (right hand slot at (-0.883,0,1.049); shaft along Y, head at -Y end)
    hx,hz=-0.883,1.049; H=[]
    H.append(tube("shaft",Wood,[(hx,0.5,hz),(hx,-1.0,hz)],[0.055,0.055],sides=6,up=(0,0,1)))
    H.append(box("pommel",Metal,(0.16,0.1,0.16),(hx,0.52,hz),bevel=0.03))
    H.append(box("grip",Leather,(0.135,0.34,0.135),(hx,0.0,hz),bevel=0.03))
    yc=-1.12
    H.append(box("collar",Metal,(0.2,0.4,0.4),(hx,yc,hz),bevel=0.035))
    for s in (1,-1):
        H.append(box("stone",Stone,(0.27,0.46,0.46),(hx+s*0.225,yc,hz),bevel=0.06))
        H.append(box("face",StoneD,(0.06,0.37,0.37),(hx+s*0.37,yc,hz),bevel=0.035))
        # veins: zigzag crack sunk into each of the four long faces (only 6 mm proud of the stone)
        for fi,(ay,az) in enumerate(((0,1),(0,-1),(1,0),(-1,0))):
            zz=[(-0.105,0.05),(-0.045,-0.055),(0.02,0.045),(0.075,-0.04),(0.11,0.02)]
            if fi%2: zz=[(u,-v) for u,v in zz]
            zz.append(None); zz+= [(0.02,0.045),(0.05,0.12)] if fi%2==0 else [(-0.045,0.055),(-0.075,0.125)]
            prev=None
            for q in zz:
                if q is None: prev=None; continue
                if prev is not None:
                    (u0,v0),(u1,v1)=prev,q
                    L=math.hypot(u1-u0,v1-v0)+0.018; ang=math.degrees(math.atan2(v1-v0,u1-u0))
                    um,vm=(u0+u1)/2,(v0+v1)/2
                    if az: c=(hx+s*(0.225+um),yc+vm,hz+az*0.221); sz=(L,0.026,0.03); rot=(0,0,s*ang)
                    else:  c=(hx+s*(0.225+um),yc+ay*0.221,hz+vm); sz=(L,0.03,0.026); rot=(0,-s*ang,0)
                    H.append(box("vein",Magic,sz,c,rot=rot))
                prev=q
    H.append(ico("moss",Moss,(0.16,0.13,0.06),(hx+0.26,yc+0.12,hz+0.235),sub=1))
    fin("Brann","weapon",H,sharp=30)
    recolor(R+"src/barbarian_texture.png",OUT+"Brann/brann_body_texture.png",{
        (0,2):"#757d8a",(1,2):"#757d8a",(6,3):"#6e4a38",(7,3):"#3b3b46",(2,2):"#dfe1e0",(7,1):"#25252e",(7,2):"#2e2e38",(3,1):"#5e4334",(0,3):"#757d8a",(1,3):"#3b3b46"})

# =============================== VEY ===============================
def vey():
    reset()
    Ear=mat("Ear","#99a1b8"); EarD=mat("EarDark","#646d8c"); Cloth=mat("Cloth","#2c5052"); Magic=mat("Magic","#9cc6cb")
    Metal=mat("Metal","#c5ccd3"); Grip=mat("Grip","#2c3150"); Guard=mat("Guard","#6f7887")
    P=[]
    Void=mat("Void","#16141e",0.95)
    # matte black dome just inside the hood opening: hides the human nose/mouth relief
    P.append(ico("void",Void,(0.94,0.98,0.74),(0,-0.05,1.52),sub=2))
    def side(s):
        o=[]
        o.append(ico("eye",Magic,(0.17,0.06,0.085),(s*0.19,-0.47,1.6),rot=(0,s*16,s*-14),sub=2))
        return o
    P+=sym(side)
    # Magic trim: thin strip following the real lip of the pack hood opening
    half=[(0,-0.59,1.79),(0.10,-0.55,1.76),(0.24,-0.485,1.715),(0.41,-0.41,1.61),(0.45,-0.38,1.48),(0.42,-0.37,1.35),(0.36,-0.37,1.28),(0.25,-0.41,1.24),(0.15,-0.42,1.23),(0,-0.41,1.21)]
    loop=half+[(-x,y,z) for x,y,z in reversed(half[1:-1])]
    cen=Vector((0,-0.3,1.5)); loop=[Vector(q)+(Vector(q)-cen).normalized()*0.012+Vector((0,-0.012,0)) for q in loop]
    n=len(loop)
    for i in range(n):
        A,B=loop[i],loop[(i+1)%n]; dd=(B-A).normalized()
        P.append(tube("trim",Magic,[A-dd*0.012,B+dd*0.012],[(0.022,0.016)]*2,sides=4,up=(0,-1,0)))
    fin("Vey","head",P,sharp=40)
    # wind funnel replacing the legs: three spiral ribbons narrowing to a point, gaps in between
    T=[]
    for j in range(3):
        pts=[];rad=[]
        N=14
        for i in range(N+1):
            t=i/N; ang=math.radians(j*120+t*430)
            Rr=0.5*(1-t)**0.85+0.015; z=0.6-0.6*t
            pts.append((1.1*Rr*math.cos(ang),-0.03+0.72*Rr*math.sin(ang)+0.1*t*t,z))
            rad.append((0.028*(1-0.5*t)+0.004,(0.085*(1-t)**0.7+0.008)))
        T.append(tube("ribbon",Magic,pts,rad,sides=4,up=(0,0,1)))
    T.append(cone("core",Magic,r1=0.01,r2=0.15,depth=0.44,loc=(0,0.05,0.02),segs=6))
    fin("Vey","tail",T,sharp=35)
    # daggers: pack knife meshes, retinted to materials and slightly curved
    bpy.ops.import_scene.fbx(filepath=R+"src/RogueHooded.fbx")
    im=bpy.data.images.load(R+"src/rogue_texture.png"); w,hh=im.size
    px=np.array(im.pixels[:]).reshape(hh,w,4)
    res={}
    for nm,part,s in (("Knife","weapon_r",-1),("Knife_Offhand","weapon_l",1)):
        src=bpy.data.objects[nm]
        me=src.data.copy(); me.transform(src.matrix_world)
        o=bpy.data.objects.new("dag",me); bpy.context.scene.collection.objects.link(o)
        if src.matrix_world.determinant()<0: me.flip_normals()
        uv=me.uv_layers.active.data; idx=[]
        for p in me.polygons:
            c=np.mean([uv[l].uv[:] for l in p.loop_indices],axis=0)
            col=px[min(hh-1,int(c[1]%1*hh)),min(w-1,int(c[0]%1*w)),:3]
            v=col.max(); sat=(col.max()-col.min())/max(v,1e-4)
            idx.append(0 if (sat<0.2 and v>0.5) else (2 if sat<0.2 else 1))
        me.materials.clear()
        for m in (Metal,Grip,Guard): me.materials.append(m)
        for p,i in zip(me.polygons,idx): p.material_index=i
        for v in me.vertices:
            d=max(0.0,(-v.co.y-0.2)/0.78)
            v.co.x+= s*0.16*d*d
        while me.uv_layers: me.uv_layers.remove(me.uv_layers[0])
        res[part]=o
    for o in list(bpy.data.objects):
        if o not in res.values(): bpy.data.objects.remove(o,do_unlink=True)
    for part,o in res.items(): fin("Vey",part,[o],sharp=40)
    recolor(R+"src/rogue_texture.png",OUT+"Vey/vey_body_texture.png",{
        (1,2):"#4a5372",(0,2):"#2e3352",(5,1):"#aab3c3",(5,3):"#2c5052",(6,3):"#233d40",(0,3):"#16141e",(1,3):"#16141e",(7,2):"#2e3352",(3,1):"#2e3352",(2,3):"#16141e"})

which=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else ["rax","liora","brann","vey"]
for wname in which: globals()[wname]()
for hero,d in INFO.items(): json.dump(d,open(OUT+f"{hero}/info.json","w"),indent=1)
