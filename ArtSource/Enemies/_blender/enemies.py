import sys, json; sys.path.insert(0,"/home/souzx/quint_heroes")
from lib import *
OUT=R+"out/Enemies/"; INFO={}
def fin(name,objs,sharp=38):
    o=join(name,objs,sharp); export(o,OUT+name+".fbx"); INFO[name]=info(o); print("PART",name,INFO[name]); return o
def crystal(m,base,d,r,L,segs=6):
    d=Vector(d).normalized(); base=Vector(base)
    return [cone_dir("cry",m,base,d,r*0.7,L*0.7,segs=segs,r2=r), cone_dir("cryt",m,base+d*L*0.7,d,r,L*0.3,segs=segs,r2=0.0)]
hx,hz=-0.883,1.049

# ============ CEIFADOR ============
reset()
Cloth=mat("Cloth","#2b303f"); ClothD=mat("ClothDark","#1d2029"); Metal=mat("Metal","#a3abb5"); MetalD=mat("MetalDark","#4a4f59"); Wood=mat("Wood","#4a3a33")
P=[]
def dome_fn(v):
    if v.z<0: v.z*=0.5
    return v
P.append(rbox("cap",Cloth,(1.02,1.06,0.72),(0,0.04,1.98),cuts=3,round=0.95,fn=dome_fn))
P.append(rbox("wrap",Cloth,(1.02,0.74,0.86),(0,0.27,1.62),cuts=2,round=0.7))
P.append(lock("peak",Cloth,(0,-0.3,2.16),(0,-1,-0.3),(0,-0.3,1),0.5,0.3,thick=0.4,r2=0.15))
P.append(tube("tail",Cloth,[(0,0.3,2.08),(0,0.75,2.2),(0,1.15,2.16),(0,1.5,1.98)],[(0.34,0.2),(0.24,0.15),(0.14,0.09),(0.012,0.01)],sides=6))
for x,ln in ((-0.36,0.34),(-0.12,0.5),(0.14,0.4),(0.38,0.52)):
    P.append(lock("tatter",ClothD,(x,0.48+0.1*(1-abs(x)/0.4),1.3),(x*0.4,0.45,-1),(x,1,0.3),0.26,ln,thick=0.2,r2=0.12))
for s in (1,-1): P.append(lock("side",ClothD,(s*0.5,0.0,1.42),(s*0.15,0.2,-1),(s,0,0.2),0.3,0.34,thick=0.2,r2=0.12))
fin("ceifador_head",P)
P=[tube("shaft",Wood,[(hx,0.75,hz),(hx,-1.6,hz)],[0.042,0.042],sides=6)]
P.append(box("collar",MetalD,(0.13,0.2,0.16),(hx,-1.5,hz+0.01),bevel=0.03))
P.append(box("butt",MetalD,(0.11,0.1,0.11),(hx,0.76,hz),bevel=0.025))
bl=[(hx+0.02,-1.5,hz),(hx+0.36,-1.66,hz+0.06),(hx+0.74,-1.58,hz+0.14),(hx+1.04,-1.32,hz+0.22),(hx+1.22,-0.95,hz+0.3),(hx+1.24,-0.6,hz+0.36)]
P.append(tube("blade",Metal,bl,[(0.15,0.028),(0.16,0.028),(0.14,0.026),(0.11,0.022),(0.07,0.018),(0.006,0.005)],sides=4,up=(0,0,1)))
P.append(tube("spine",MetalD,[(p[0]-0.03*(i>=3),p[1]-0.1*(1 if i<3 else 0.5),p[2]) for i,p in enumerate(bl[:5])],[(0.045,0.04)]*4+[(0.01,0.01)],sides=4,up=(0,0,1)))
P.append(box("wrap",Cloth,(0.1,0.36,0.1),(hx,0.0,hz),bevel=0.02))
fin("ceifador_weapon",P)
P=[rbox("knot",Cloth,(0.7,0.22,0.22),(0,0.33,1.22),cuts=2,round=0.6)]
for x,ln,dz,wd in ((-0.3,0.95,-0.25,0.3),(-0.08,1.25,-0.1,0.34),(0.16,1.05,-0.32,0.3),(0.34,0.8,-0.05,0.26)):
    P.append(lock("rag",Cloth if x<0.1 else ClothD,(x,0.34,1.18),(x*0.5,1,dz),(0,0.2,1),wd,ln,thick=0.14,r2=0.1))
fin("ceifador_back",P)

# ============ ARREMESSADOR ============
reset()
Leather=mat("Leather","#6b4a36"); LeatherD=mat("LeatherDark","#4a3226"); Cloth=mat("Cloth","#8a4030"); Metal=mat("Metal","#9a7b45"); Lens=mat("Lens","#1c2226")
Wicker=mat("Wicker","#8a6a42"); WickerD=mat("WickerDark","#65492c"); Bomb=mat("Bomb","#1d1c20"); Fuse=mat("Fuse","#b99a6a"); Ember=mat("Ember","#c8672f")
def bomb(c,r,fd=(0.2,0.1,1)):
    c=Vector(c); fd=Vector(fd).normalized()
    o=[ico("bomb",Bomb,(2*r,2*r,2*r),c,sub=2)]
    o.append(cone_dir("neck",Metal,c+fd*r*0.85,fd,r*0.3,r*0.3,segs=6,r2=r*0.3))
    o.append(tube("fuse",Fuse,[c+fd*r*1.1,c+fd*r*1.45+Vector((0.03,0,0)),c+fd*r*1.75+Vector((0.07,0.02,0))],[0.022,0.02,0.018],sides=4))
    o.append(ico("ember",Ember,(0.07,0.07,0.07),c+fd*r*1.8+Vector((0.075,0.02,0)),sub=1))
    return o
P=[]
brim=cone("brim",Leather,r1=0.68,r2=0.63,depth=0.07,loc=(0,0.02,2.0),segs=12); P.append(brim)
P.append(rbox("crown",Leather,(0.86,0.9,0.5),(0,0.02,2.2),cuts=3,round=0.9,fn=dome_fn))
P.append(cone("band",Cloth,r1=0.455,r2=0.44,depth=0.1,loc=(0,0.02,2.07),segs=12,scale=(1,1.04,1)))
P.append(lock("knot",Cloth,(0.3,0.42,2.1),(0.5,1,-0.5),(0,0.3,1),0.18,0.3,thick=0.25,r2=0.2))
for s in (1,-1):
    P.append(cone_dir("gog",Metal,(s*0.2,-0.42,1.92),(0,-1,0.25),0.135,0.1,segs=8,r2=0.125))
    P.append(cone_dir("lens",Lens,(s*0.2,-0.5,1.94),(0,-1,0.25),0.1,0.02,segs=8,r2=0.09))
P.append(box("bridge",LeatherD,(0.2,0.06,0.07),(0,-0.45,1.93),bevel=0.015))
P.append(cone("strap",LeatherD,r1=0.47,r2=0.47,depth=0.08,loc=(0,0.0,1.88),segs=10,scale=(1,1.02,1)))
fin("arremessador_head",P)
P=[]
P.append(cone("basket",Wicker,r1=0.3,r2=0.38,depth=0.56,loc=(0,0.7,0.78),segs=8))
P.append(cone("rimb",WickerD,r1=0.4,r2=0.4,depth=0.07,loc=(0,0.7,1.31),segs=8))
P.append(cone("hoop",WickerD,r1=0.345,r2=0.355,depth=0.06,loc=(0,0.7,1.01),segs=8))
P+=bomb((-0.17,0.62,1.44),0.2,(-0.3,0.1,1)); P+=bomb((0.18,0.65,1.46),0.2,(0.3,0.0,1)); P+=bomb((0.0,0.88,1.5),0.2,(0.0,0.4,1))
for s in (1,-1): P.append(tube("strapb",LeatherD,[(s*0.26,0.44,1.26),(s*0.3,0.1,1.34),(s*0.3,-0.2,1.3),(s*0.26,-0.33,1.05)],[(0.05,0.02)]*4,sides=4,up=(0,0,1)))
fin("arremessador_back",P,sharp=30)
P=bomb((hx,-0.04,hz+0.1),0.22,(0.1,-0.25,1)); fin("arremessador_weapon",P,sharp=30)

# ============ XAMA ============
reset()
Bone=mat("Bone","#ddd2b8"); BoneD=mat("BoneDark","#a8997c"); Socket=mat("Socket","#1a1614"); Antler=mat("Antler","#8a6f52"); Feather=mat("Feather","#2f5e58"); FeatherT=mat("FeatherTip","#a8563a")
Wood=mat("Wood","#5a4334"); WoodD=mat("WoodDark","#3f2e25"); Cord=mat("Cord","#8a3b30"); Magic=mat("Magic","#5fb07a")
def feather(base,d,nrm,ln,wd=0.2):
    base=Vector(base); d=Vector(d).normalized()
    return [lock("fea",Feather,base,d,nrm,wd,ln*0.7,thick=0.14,r2=0.8), lock("feat",FeatherT,base+d*ln*0.7,d,nrm,wd*0.8,ln*0.38,thick=0.14,r2=0.1)]
P=[]
def sn(v):
    t=max(0,-v.y); v.x*=1-0.45*t; v.z*=1-0.3*t; return v
P.append(rbox("mask",Bone,(0.72,0.36,0.56),(0,-0.42,1.78),cuts=2,round=0.92))
P.append(rbox("snout",Bone,(0.4,0.7,0.32),(0,-0.68,1.5),rot=(-28,0,0),cuts=3,round=0.85,fn=sn))
P.append(box("nose",Socket,(0.12,0.05,0.08),(0,-0.985,1.345),rot=(-28,0,0),bevel=0.02))
for s in (1,-1):
    P.append(ico("socket",Socket,(0.2,0.08,0.24),(s*0.2,-0.6,1.78),rot=(0,0,s*-12),sub=1))
    a0=(s*0.2,-0.3,2.02); a1=(s*0.42,-0.22,2.4); a2=(s*0.72,-0.12,2.62); a3=(s*0.78,-0.05,2.95)
    P.append(tube("antler",Antler,[a0,a1,a2,a3],[0.07,0.06,0.05,0.02],sides=5))
    P.append(tube("tine",Antler,[a1,(s*0.34,-0.3,2.62),(s*0.22,-0.34,2.84)],[0.05,0.04,0.015],sides=5))
    P.append(tube("tine",Antler,[a2,(s*0.98,-0.1,2.72),(s*1.12,-0.04,2.9)],[0.045,0.035,0.015],sides=5))
    P.append(tube("cordm",Cord,[(s*0.36,-0.3,1.8),(s*0.47,0.0,1.84),(s*0.36,0.4,1.86)],[(0.03,0.05)]*3,sides=4,up=(0,0,1)))
P.append(tube("cordm",Cord,[(-0.36,0.4,1.86),(0,0.5,1.88),(0.36,0.4,1.86)],[(0.03,0.05)]*3,sides=4,up=(0,0,1)))
for a,ln in ((-50,0.55),(-25,0.7),(0,0.8),(25,0.7),(50,0.55)):
    t=math.radians(a); P+=feather((0.3*math.sin(t),0.42,1.9),(math.sin(t)*0.8,0.75,0.55*math.cos(t)),(0,-0.6,1),ln,0.22)
fin("xama_head",P)
P=[tube("shaft",Wood,[(hx,0.85,hz),(hx,-1.25,hz)],[0.045,0.05],sides=6)]
P.append(box("tot1",WoodD,(0.24,0.26,0.24),(hx,-1.25,hz),bevel=0.04))
P.append(box("tot2",Wood,(0.3,0.16,0.3),(hx,-1.44,hz),bevel=0.04))
for s in (1,-1):
    P.append(tube("horn",Bone,[(hx+s*0.12,-1.5,hz),(hx+s*0.3,-1.66,hz),(hx+s*0.3,-1.9,hz),(hx+s*0.16,-2.06,hz)],[0.055,0.05,0.04,0.012],sides=5))
    P.append(ico("eye",Socket,(0.08,0.05,0.1),(hx+s*0.07,-1.2,hz+0.125),sub=1))
P.append(ico("stone",Magic,(0.34,0.36,0.34),(hx,-1.78,hz),sub=1))
P.append(box("cordw",Cord,(0.13,0.12,0.13),(hx,-1.08,hz),bevel=0.02))
P+=feather((hx+0.06,-1.1,hz-0.05),(0.5,0.25,-1),(0,1,0.2),0.42,0.14); P+=feather((hx-0.06,-1.1,hz-0.05),(-0.45,0.4,-1),(0,1,0.2),0.34,0.13)
P.append(tube("charm",BoneD,[(hx,-1.1,hz+0.05),(hx+0.02,-1.0,hz+0.3)],[0.035,0.02],sides=4))
P.append(box("grip",Cord,(0.11,0.3,0.11),(hx,0.0,hz),bevel=0.02))
fin("xama_weapon",P,sharp=30)
P=[]
for i in range(9):
    a=math.radians(-100+i*25+180+90)  # arc across shoulders and back, open at the front
    a=math.radians(200+i*(140/8)) if False else math.radians(-20+i*27.5)
    x=0.5*math.cos(a); y=0.08+0.42*math.sin(a)
    out=Vector((math.cos(a),math.sin(a),0))
    if i%2==0: P.append(cone_dir("bonec",Bone,(x,y,1.3),out+Vector((0,0,-0.45)),0.06,0.3,segs=5,r2=0.012))
    else: P+=feather((x,y,1.32),out+Vector((0,0,-0.25)),(0,0,1),0.42,0.18)
P.append(tube("cordc",Cord,[(0.5*math.cos(math.radians(a)),0.08+0.42*math.sin(math.radians(a)),1.32) for a in range(-25,206,23)],[(0.035,0.035)]*11,sides=4,up=(0,0,1)))
fin("xama_back",P)

# ============ CAMPEAO ============
reset()
Crystal=mat("Crystal","#cf7c2b"); Metal=mat("Metal","#4a4c52")
ZC=2.9
P=[tube("ring",Metal,[(0.4*math.cos(math.radians(a)),0.4*math.sin(math.radians(a)),ZC) for a in (i*30,i*30+30)],[(0.035,0.03)]*2,sides=4,up=(0,0,1)) for i in range(12)]
for i in range(6):
    a=math.radians(i*60+30); o=Vector((math.cos(a),math.sin(a),0)); L=0.42 if i%2==0 else 0.3
    P+=crystal(Crystal,Vector((0.4*o.x,0.4*o.y,ZC-0.06)),o*0.32+Vector((0,0,1)),0.085 if i%2==0 else 0.07,L)
fin("campeao_crown",P)
json.dump(INFO,open(OUT+"info.json","w"),indent=1)
