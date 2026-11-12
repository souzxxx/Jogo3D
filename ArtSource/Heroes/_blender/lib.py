import bpy, bmesh, math, numpy as np, os
from mathutils import Vector, Matrix, Euler
R="/home/souzx/quint_heroes/"
def hexc(h):
    h=h.lstrip('#'); c=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return [x/12.92 if x<=0.04045 else ((x+0.055)/1.055)**2.4 for x in c]+[1.0]
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name,hx,rough=0.8):
    m=bpy.data.materials.get(name)
    if m is None:
        m=bpy.data.materials.new(name); m.use_nodes=True
    b=m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value=hexc(hx); b.inputs["Roughness"].default_value=rough
    b.inputs["Metallic"].default_value=0.0
    try: b.inputs["Specular IOR Level"].default_value=0.2
    except Exception: pass
    m.diffuse_color=hexc(hx)
    return m
def new_obj(name,bm,material=None):
    me=bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    if material: me.materials.append(material)
    return o
def xf(o,loc=(0,0,0),rot=(0,0,0),scale=(1,1,1)):
    M=Matrix.Translation(loc)@Euler([math.radians(a) for a in rot],'XYZ').to_matrix().to_4x4()@Matrix.Diagonal((*scale,1))
    o.data.transform(M); o.data.update(); return o
def rbox(name,m,size=(1,1,1),loc=(0,0,0),rot=(0,0,0),cuts=3,round=0.6,fn=None):
    """rounded box: cube subdivided and partially spherized. fn(v)->v deforms in unit space (-1..1) before scaling"""
    bm=bmesh.new(); bmesh.ops.create_cube(bm,size=2.0)
    if cuts: bmesh.ops.subdivide_edges(bm,edges=bm.edges[:],cuts=cuts,use_grid_fill=True)
    for v in bm.verts:
        s=v.co.normalized()*1.18
        v.co=v.co.lerp(s,round)/(1+0.18*round)
        if fn: v.co=Vector(fn(v.co.copy()))
    o=new_obj(name,bm,m)
    return xf(o,loc,rot,[s/2 for s in size])
def ico(name,m,size=(1,1,1),loc=(0,0,0),rot=(0,0,0),sub=1,fn=None):
    bm=bmesh.new(); bmesh.ops.create_icosphere(bm,subdivisions=sub,radius=1.0)
    if fn:
        for v in bm.verts: v.co=Vector(fn(v.co.copy()))
    o=new_obj(name,bm,m); return xf(o,loc,rot,[s/2 for s in size])
def cone(name,m,r1=0.1,r2=0.0,depth=0.3,loc=(0,0,0),rot=(0,0,0),segs=6,scale=(1,1,1)):
    """cone along +Z, base (r1) at z=0, tip (r2) at z=depth"""
    bm=bmesh.new(); bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=segs,radius1=r1,radius2=max(r2,0.0001),depth=depth)
    for v in bm.verts: v.co.z+=depth/2
    bmesh.ops.remove_doubles(bm,verts=bm.verts[:],dist=0.0005)
    o=new_obj(name,bm,m); return xf(o,loc,rot,scale)
def cone_dir(name,m,base,d,r1,length,segs=5,flat=(1,1),r2=0.0):
    o=cone(name,m,r1=r1,r2=r2,depth=length,segs=segs,scale=(flat[0],flat[1],1))
    q=Vector(d).normalized().to_track_quat('Z','Y')
    o.data.transform(Matrix.Translation(base)@q.to_matrix().to_4x4()); return o
def lock(name,m,base,d,normal,width,length,thick=0.3,r2=0.2,segs=4):
    """flat tapered lock: long axis d, thin axis ~normal"""
    o=cone(name,m,r1=width/2,r2=width/2*r2,depth=length,segs=segs)
    z=Vector(d).normalized(); y=Vector(normal)-z*Vector(normal).dot(z); y.normalize(); x=y.cross(z)
    Mx=Matrix((x,y,z)).transposed().to_4x4()
    o.data.transform(Matrix.Translation(base)@Mx@Matrix.Diagonal((1,thick,1,1))@Matrix.Rotation(math.pi/segs,4,'Z')); return o
def box(name,m,size=(1,1,1),loc=(0,0,0),rot=(0,0,0),bevel=0.0,taper=None):
    bm=bmesh.new(); bmesh.ops.create_cube(bm,size=1.0)
    for v in bm.verts:
        v.co.x*=size[0]; v.co.y*=size[1]; v.co.z*=size[2]
        if taper:  # (sx,sy) scale at +z end
            t=(v.co.z/size[2]+0.5); v.co.x*=1+(taper[0]-1)*t; v.co.y*=1+(taper[1]-1)*t
    if bevel>0: bmesh.ops.bevel(bm,geom=bm.edges[:],offset=bevel,segments=1,affect='EDGES',profile=0.5)
    o=new_obj(name,bm,m); return xf(o,loc,rot)
def tube(name,m,pts,radii,sides=6,flat=1.0,cap_tip=True,twist=0.0,up=(0,0,1)):
    """sweep a ring along pts. radii per point (float or (rx,ry))."""
    bm=bmesh.new(); pts=[Vector(p) for p in pts]; rings=[]
    prev_n=None
    for i,p in enumerate(pts):
        if i==0: t=(pts[1]-p)
        elif i==len(pts)-1: t=(p-pts[i-1])
        else: t=(pts[i+1]-pts[i-1])
        t.normalize()
        n=Vector(up)-t*Vector(up).dot(t) if prev_n is None else prev_n-t*prev_n.dot(t)
        if n.length<1e-4: n=Vector((1,0,0))-t*t.x
        n.normalize(); prev_n=n; b=t.cross(n)
        r=radii[i]; rx,ry=(r if isinstance(r,(tuple,list)) else (r,r*flat))
        ring=[]
        for k in range(sides):
            a=2*math.pi*k/sides+twist*i
            ring.append(bm.verts.new(p+n*math.cos(a)*ry+b*math.sin(a)*rx))
        rings.append(ring)
    for i in range(len(rings)-1):
        for k in range(sides):
            a,b2=rings[i],rings[i+1]
            bm.faces.new((a[k],a[(k+1)%sides],b2[(k+1)%sides],b2[k]))
    bm.faces.new(list(reversed(rings[0])))
    if cap_tip: bm.faces.new(rings[-1])
    bmesh.ops.remove_doubles(bm,verts=bm.verts[:],dist=0.0008)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:])
    return new_obj(name,bm,m)
def bez(p0,p1,p2,p3,n):
    out=[]
    for i in range(n+1):
        t=i/n;u=1-t
        out.append(tuple(u*u*u*p0[j]+3*u*u*t*p1[j]+3*u*t*t*p2[j]+t*t*t*p3[j] for j in range(3)))
    return out
def mirror(o,name=None):
    c=o.copy(); c.data=o.data.copy(); c.name=name or o.name+"_m"; bpy.context.scene.collection.objects.link(c)
    c.data.transform(Matrix.Diagonal((-1,1,1,1))); c.data.flip_normals(); return c
def paint(o,fn):
    """fn(face_center)->material or None ; assigns slot"""
    for p in o.data.polygons:
        m=fn(p.center)
        if m is None: continue
        if m.name not in [x.name for x in o.data.materials]: o.data.materials.append(m)
        p.material_index=[x.name for x in o.data.materials].index(m.name)
def join(name,objs,sharp=38):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    if len(objs)>1: bpy.ops.object.join()
    o=bpy.context.view_layer.objects.active; o.name=name; o.data.name=name
    bm=bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:])
    for f in bm.faces: f.smooth=True
    for e in bm.edges:
        if len(e.link_faces)==2:
            e.smooth = e.calc_face_angle(0) < math.radians(sharp) and e.link_faces[0].material_index==e.link_faces[1].material_index
    bmesh.ops.triangulate(bm,faces=bm.faces[:])
    bm.to_mesh(o.data); bm.free()
    # drop unused material slots
    used=sorted({p.material_index for p in o.data.polygons})
    mats=[o.data.materials[i] for i in used]; remap={u:i for i,u in enumerate(used)}
    idx=[remap[p.material_index] for p in o.data.polygons]
    o.data.materials.clear()
    for m in mats: o.data.materials.append(m)
    for p,i in zip(o.data.polygons,idx): p.material_index=i
    return o
def info(o):
    vs=[v.co for v in o.data.vertices]
    mn=[round(min(v[i] for v in vs),3) for i in range(3)]; mx=[round(max(v[i] for v in vs),3) for i in range(3)]
    return dict(tris=len(o.data.polygons),min=mn,max=mx,mats=[m.name for m in o.data.materials])
def export(o,path):
    os.makedirs(os.path.dirname(path),exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,
        mesh_smooth_type='EDGE',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO',use_tspace=False)
def recolor(src,dst,cells,default=None):
    """cells: {(col,rowFromBottom): hex or (hexDark,hexLight)} atlas 8x4."""
    im=bpy.data.images.load(src); w,h=im.size
    px=np.array(im.pixels[:],dtype=np.float32).reshape(h,w,4)
    cw,ch=w//8,h//4
    def rgb(hx): hx=hx.lstrip('#'); return np.array([int(hx[i:i+2],16)/255 for i in (0,2,4)],dtype=np.float32)
    for (c,r),tgt in cells.items():
        reg=px[r*ch:(r+1)*ch,c*cw:(c+1)*cw,:3]
        L=reg@np.array([0.3,0.59,0.11],dtype=np.float32)
        lo,hi=np.percentile(L,2),np.percentile(L,98)
        t=np.clip((L-lo)/max(hi-lo,1e-4),0,1)[...,None]
        if isinstance(tgt,str): base=rgb(tgt); d,l=base*0.80,np.clip(base*1.12,0,1)
        else: d,l=rgb(tgt[0]),rgb(tgt[1])
        reg[...]=d+(l-d)*t
    out=bpy.data.images.new("out",w,h,alpha=True); out.pixels=px.ravel(); out.filepath_raw=dst; out.file_format='PNG'; out.save()
    bpy.data.images.remove(im); bpy.data.images.remove(out)

# ---------------- preview ----------------
def load_body(fbx,texpath,hide,offset=(0,0,0),suffix=""):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx)
    new=[o for o in bpy.data.objects if o not in before]
    arm=[o for o in new if o.type=='ARMATURE'][0]
    arm.animation_data_clear()
    root=bpy.data.objects.new("Root"+suffix,None); bpy.context.scene.collection.objects.link(root)
    arm.parent=root; root.location=offset; bpy.context.view_layer.update()
    for o in new:
        if o.type=='MESH':
            if o.name.split('.')[0] in hide or (o.parent_type=='BONE' and 'Cape' not in o.name): o.hide_render=True; o.hide_viewport=True
            for m in o.data.materials:
                if m and m.use_nodes:
                    for n in m.node_tree.nodes:
                        if n.type=='TEX_IMAGE' and texpath:
                            n.image=bpy.data.images.load(texpath,check_existing=True); n.interpolation='Closest'
                        if n.type=='BSDF_PRINCIPLED':
                            n.inputs["Roughness"].default_value=0.8
                            try: n.inputs["Specular IOR Level"].default_value=0.2
                            except Exception: pass
    return arm,new
def attach(arm,fbx,bone):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx)
    new=[o for o in bpy.data.objects if o not in before]
    for o in new:
        mw=o.matrix_world.copy()
        o.parent=arm; o.parent_type='BONE'; o.parent_bone=bone
        o.matrix_world=mw
    bpy.context.view_layer.update()
    for o in new:
        pass
    return new
def keep_world(objs,mws):
    for o,mw in zip(objs,mws): o.matrix_world=mw
def pose(arm,action,frame):
    act=bpy.data.actions.get(action)
    if act is None: print("NO ACTION",action); return
    if not arm.animation_data: arm.animation_data_create()
    arm.animation_data.action=act
    try: arm.animation_data.action_slot=act.slots[0]
    except Exception as e: print("slot",e)
    bpy.context.scene.frame_set(frame)
def setup_scene(res=900,samples=48):
    sc=bpy.context.scene
    sc.render.engine='CYCLES'
    try:
        prefs=bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type='OPTIX'; prefs.get_devices()
        for d in prefs.devices: d.use = d.type!='CPU'
        sc.cycles.device='GPU'
    except Exception as e: print("gpu",e)
    sc.cycles.samples=samples; sc.cycles.use_denoising=True
    sc.render.resolution_x=res; sc.render.resolution_y=res
    sc.view_settings.view_transform='Standard'; sc.view_settings.look='None'
    w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True
    w.node_tree.nodes["Background"].inputs[0].default_value=(0.75,0.75,0.78,1); w.node_tree.nodes["Background"].inputs[1].default_value=0.55
    bm=bmesh.new(); bmesh.ops.create_grid(bm,x_segments=1,y_segments=1,size=40)
    fl=new_obj("Floor",bm,mat("FloorMat","#6b5f55",0.9))
    sun=bpy.data.lights.new("Sun",'SUN'); sun.energy=2.6; sun.angle=math.radians(25)
    so=bpy.data.objects.new("Sun",sun); sc.collection.objects.link(so); so.rotation_euler=(math.radians(50),0,math.radians(-35))
    cam=bpy.data.cameras.new("Cam"); co=bpy.data.objects.new("Cam",cam); sc.collection.objects.link(co); sc.camera=co
    return co
def aim(co,loc,target,lens=50,ortho=None):
    co.location=loc
    d=Vector(target)-Vector(loc); co.rotation_euler=d.to_track_quat('-Z','Y').to_euler()
    co.data.lens=lens
    if ortho: co.data.type='ORTHO'; co.data.ortho_scale=ortho
    else: co.data.type='PERSP'
def render(path):
    bpy.context.scene.render.filepath=path; bpy.ops.render.render(write_still=True)
VIEWS={"front":((3.4,-6.2,3.3),(0,0,1.45),82),"back":((-3.4,6.2,3.3),(0,0.3,1.45),82),"top":((0,-7.46,11.65),(0,0.1,1.1),95)}
