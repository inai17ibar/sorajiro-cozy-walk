"""Original editable twilight room geometry. Blender 5.2; dry-run unless --write."""
import argparse
import math
import random
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/Pipeline'))
from art_contract import preflight,finish,sha
OUTPUT=ROOT/'Artifacts/SourceArt/Room/twilight_v005'
RECIPE='room-twilight-v005-'+sha(__file__)

def main():
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--write',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    reuse=preflight(OUTPUT,RECIPE); print('ROOM PREFLIGHT', 'REUSE' if reuse else 'CREATE',flush=True)
    if reuse or not args.write:return
    import bpy
    from mathutils import Vector
    if bpy.app.version[:2]!=(5,2):raise RuntimeError('Review Blender version')
    OUTPUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene; scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    specs={'Oak':(.31,.18,.085),'OakLight':(.47,.30,.16),'Wall':(.46,.35,.29),'Trim':(.69,.60,.48),
           'BlueTextile':(.18,.31,.48),'IvoryTextile':(.71,.65,.53),'Rug':(.60,.55,.47),
           'Curtain':(.51,.39,.36),'BookBlue':(.13,.27,.43),'BookCream':(.70,.59,.39),
           'BookRose':(.43,.23,.24),'Pages':(.71,.65,.52),'Leaf':(.11,.23,.085),
           'Pot':(.37,.24,.16),'Gold':(.71,.41,.10),'StarGlow':(1,.63,.18),'LampShade':(.81,.61,.32)}
    mats={}
    for name,color in specs.items():
        m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); mats[name]=m
    objects=[]
    def coord(p): x,y,z=p; return (x,-z,y)
    def material(obj,name):
        obj.data.materials.append(mats[name]); objects.append(obj); return obj
    def box(name,p,size,mat='Oak',bevel=.012):
        bpy.ops.mesh.primitive_cube_add(size=1,location=coord(p)); obj=bpy.context.object; obj.name=name
        obj.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        if bevel:
            mod=obj.modifiers.new('SoftEdges','BEVEL'); mod.width=min(bevel,min(size)*.3); mod.segments=3
            bpy.ops.object.modifier_apply(modifier=mod.name)
            mod=obj.modifiers.new('WeightedNormals','WEIGHTED_NORMAL'); bpy.ops.object.modifier_apply(modifier=mod.name)
        return material(obj,mat)
    def ball(name,p,size,mat='IvoryTextile'):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=coord(p)); o=bpy.context.object; o.name=name
        o.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        for f in o.data.polygons:f.use_smooth=True
        return material(o,mat)
    def mesh(name,verts,faces,mat):
        data=bpy.data.meshes.new(name); data.from_pydata([coord(p) for p in verts],[],faces); data.update()
        obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj)
        for f in data.polygons:f.use_smooth=True
        return material(obj,mat)
    def line(name,points,radius,mat):
        data=bpy.data.curves.new(name,'CURVE'); data.dimensions='3D'; data.bevel_depth=radius; data.bevel_resolution=2
        spline=data.splines.new('POLY'); spline.points.add(len(points)-1)
        for p,point in zip(spline.points,points):p.co=(*coord(point),1)
        obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj)
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
        bpy.ops.object.convert(target='MESH'); return material(bpy.context.object,mat)
    def star(name,p,r,thick=.018):
        x,y,z=p; verts=[]
        for dz in (-thick/2,thick/2):
            for i in range(10):
                a=math.pi/2+math.tau*i/10; rad=r if i%2==0 else r*.48
                verts.append((x+math.cos(a)*rad,y+math.sin(a)*rad,z+dz))
        faces=[tuple(reversed(range(10))),tuple(range(10,20))]
        faces.extend((i,(i+1)%10,(i+1)%10+10,i+10) for i in range(10))
        obj=mesh(name,verts,faces,'StarGlow')
        mod=obj.modifiers.new('SoftStarEdge','BEVEL'); mod.width=.004; mod.segments=3
        bpy.context.view_layer.objects.active=obj; bpy.ops.object.modifier_apply(modifier=mod.name)
        return obj
    rng=random.Random(607)
    # Full 3D room, with an actual opening rather than a reference-image backdrop.
    for row in range(21):
        z=-2.0+row*.215; start=-2.50-(row%3)*.39
        while start<2.5:
            end=min(2.5,start+1.17); left=max(-2.5,start)
            if end-left>.015:box('FloorBoard',((left+end)/2,-.025,z),(end-left-.003,.045,.212),'OakLight',.0015)
            start+=1.17
    box('BackWallLeft',(-1.50,1.3,2.3),(1.9,2.6,.12),'Wall')
    box('BackWallRight',(1.80,1.3,2.3),(1.3,2.6,.12),'Wall')
    box('WindowWallBelow',(.35,.32,2.3),(1.8,.64,.12),'Wall')
    box('WindowWallAbove',(.35,2.40,2.3),(1.8,.40,.12),'Wall')
    box('LeftWall',(-2.50,1.3,.25),(.12,2.6,4.2),'Wall')
    box('RightWall',(2.50,1.3,.25),(.12,2.6,4.2),'Wall')
    for x in (-.53,1.23):box('WindowFrame',(x,1.4,2.25),(.065,1.62,.09),'Trim')
    for y in (.62,1.42,2.18):box('WindowCrossbar',(.35,y,2.24),(1.84,.055,.10),'Trim')
    box('WindowMullion',(.35,1.4,2.24),(.055,1.62,.1),'Trim')
    box('WindowSill',(.35,.62,2.16),(2.0,.08,.32),'Trim')
    for x in (-2.42,2.42):box('Baseboard',(x,.065,.25),(.06,.13,4.2),'Trim',.005)
    box('BackBaseboard',(0,.065,2.2),(4.9,.13,.06),'Trim',.005)
    # Draped curtains with pleats and a scalloped hem.
    for cx in (-.71,1.42):
        verts=[]; faces=[]; nx=36; ny=60
        for j in range(ny+1):
            t=j/ny
            for i in range(nx+1):
                u=i/nx; x=cx+(u-.5)*.36
                y=.12+2.17*t+.018*math.sin(u*math.tau*6)*(1-t)**8
                z=2.08+.035*math.cos(u*math.tau*6)+.03*math.sin(t*math.pi)
                verts.append((x,y,z))
        for j in range(ny):
            for i in range(nx):
                a=j*(nx+1)+i; faces.append((a,a+1,a+nx+2,a+nx+1))
        o=mesh('PleatedCurtain',verts,faces,'Curtain'); mod=o.modifiers.new('FabricThickness','SOLIDIFY'); mod.thickness=.002
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    # Bed with rounded posts, mattress, a deformed quilt, pillow and folded throw.
    for x in (-2.05,-.85):
        for z in (-.20,1.75):
            box('BedPost',(x,.39,z),(.075,.78,.075),'Oak')
            ball('BedPostCap',(x,.80,z),(.05,.05,.05),'OakLight')
    box('BedRail',(-1.45,.28,.77),(1.26,.17,2.04),'Oak')
    box('Mattress',(-1.45,.43,.77),(1.22,.19,1.98),'IvoryTextile',.05)
    box('Headboard',(-1.45,.70,1.78),(1.28,.58,.07),'Oak')
    # Gravity drape in real Blender cloth; exported mesh has no Unity runtime cloth cost.
    mattress=bpy.data.objects.get('Mattress')
    mattress.modifiers.new('QuiltCollision','COLLISION')
    mattress.collision.thickness_outer=.003
    mattress.collision.cloth_friction=12
    verts=[]; faces=[]; nx=48; nz=64
    for j in range(nz+1):
        z=-.38+1.70*j/nz
        for i in range(nx+1):
            x=-2.24+1.58*i/nx
            verts.append((x,.64+.003*math.sin(x*23+z*11),z))
    for j in range(nz):
        for i in range(nx):
            a=j*(nx+1)+i; faces.append((a,a+nx+1,a+nx+2,a+1))
    quilt=mesh('Quilt_GRAVITY_DRAPED',verts,faces,'BlueTextile')
    uv=quilt.data.uv_layers.new(name='UVMap')
    for loop in quilt.data.loops:
        index=loop.vertex_index;uv.data[loop.index].uv=((index%(nx+1))/nx,(index//(nx+1))/nz)
    pin=quilt.vertex_groups.new(name='HeadEndHeld')
    pin.add([nz*(nx+1)+i for i in range(7,nx-6)],1,'REPLACE')
    cloth=quilt.modifiers.new('GravityAndMattress','CLOTH')
    cloth.settings.quality=10; cloth.settings.mass=.28
    cloth.settings.tension_stiffness=12; cloth.settings.compression_stiffness=12
    cloth.settings.shear_stiffness=8; cloth.settings.bending_stiffness=.7
    cloth.settings.air_damping=3; cloth.settings.vertex_group_mass=pin.name
    cloth.settings.pin_stiffness=1
    cloth.collision_settings.use_collision=True; cloth.collision_settings.distance_min=.004
    cloth.collision_settings.collision_quality=6
    cloth.collision_settings.use_self_collision=False
    cloth.point_cache.frame_start=1; cloth.point_cache.frame_end=110
    scene.frame_start=1; scene.frame_end=110; scene.render.fps=24
    for frame in range(1,111):
        scene.frame_set(frame)
        evaluated=quilt.evaluated_get(bpy.context.evaluated_depsgraph_get())
        if frame%20==0:print('CLOTH FRAME',frame, 'vertices',len(evaluated.data.vertices),flush=True)
    bpy.context.view_layer.objects.active=quilt
    bpy.ops.object.modifier_apply(modifier=cloth.name)
    draped=[v.co.copy() for v in quilt.data.vertices]
    if min(v.z for v in draped)>.53:raise RuntimeError('Drape did not fall below mattress top')
    if min(v.z for v in draped)<.05:raise RuntimeError('Quilt fell off mattress')
    # Slight padded quilting follows the simulated grid, rather than floating cloud spheres.
    for j in range(nz+1):
        for i in range(nx+1):
            v=quilt.data.vertices[j*(nx+1)+i]
            loft=.018*(math.sin(math.pi*(i%8)/8)**2)*(math.sin(math.pi*(j%8)/8)**2)
            v.co+=v.normal*loft
    quilt.data.update()
    def unity_coord(v):return (v.x,v.z,-v.y)
    seam_paths=[]
    for j in range(0,nz+1,8):
        seam_paths.append([unity_coord(quilt.data.vertices[j*(nx+1)+i].co+quilt.data.vertices[j*(nx+1)+i].normal*.008) for i in range(nx+1)])
    for i in range(0,nx+1,8):
        seam_paths.append([unity_coord(quilt.data.vertices[j*(nx+1)+i].co+quilt.data.vertices[j*(nx+1)+i].normal*.008) for j in range(nz+1)])
    edges=[list(range(nx+1)),[j*(nx+1)+nx for j in range(nz+1)],list(range(nz*(nx+1),(nz+1)*(nx+1))),[j*(nx+1) for j in range(nz+1)]]
    for edge in edges:line('QuiltBoundHem',[unity_coord(quilt.data.vertices[k].co) for k in edge],.007,'BlueTextile')
    thick=quilt.modifiers.new('PaddedFabricThickness','SOLIDIFY'); thick.thickness=.012; thick.offset=0
    bpy.context.view_layer.objects.active=quilt; bpy.ops.object.modifier_apply(modifier=thick.name)
    sub=quilt.modifiers.new('SoftClothSurface','SUBSURF'); sub.levels=1
    bpy.ops.object.modifier_apply(modifier=sub.name)
    # Reproject channels onto the final thick/subdivided surface; old lines floated off hems.
    for points in seam_paths:
        projected=[]
        for point in points:
            ok,p,n,_=quilt.closest_point_on_mesh(Vector(coord(point)))
            if not ok:raise RuntimeError('Final quilting projection failed')
            projected.append(unity_coord(p+n*.00025))
        line('QuiltStitchedChannel',projected,.00045,'IvoryTextile')
    scene['cloth_bake']='110 real Blender frames at 24Hz, gravity, mattress collision, pinned head end, 12mm thickness. Static exported mesh, not runtime cloth.'
    scene['cloth_min_height_m']=min(v.z for v in draped)
    # A compressed rectangular cushion, with a sewn perimeter instead of an ellipsoid.
    verts=[]; faces=[]; rings=24; sides=64
    def sp(v,e):return math.copysign(abs(v)**e,v)
    for j in range(rings+1):
        lat=-math.pi/2+math.pi*j/rings
        for i in range(sides):
            t=math.tau*i/sides
            verts.append((-1.45+.48*sp(math.cos(lat),.48)*sp(math.cos(t),.48),
                          .61+.082*sp(math.sin(lat),.85),1.48+.23*sp(math.cos(lat),.48)*sp(math.sin(t),.48)))
    for j in range(rings):
        for i in range(sides):
            a=j*sides+i; b=j*sides+(i+1)%sides; faces.append((a,b,b+sides,a+sides))
    mesh('CompressedPillow',verts,[tuple(reversed(f)) for f in faces],'IvoryTextile')
    line('PillowPiping',[(-1.45+.479*sp(math.cos(t*math.tau/128),.48),.61,1.48+.229*sp(math.sin(t*math.tau/128),.48)) for t in range(129)],.0025,'BlueTextile')
    # Open shelves and separate books/pages, scaled above the bear.
    def shelf(cx,cz,w,h,depth,levels):
        for dx in (-w/2,w/2):box('ShelfSide',(cx+dx,h/2+.04,cz),(.045,h,depth),'Oak')
        box('ShelfBack',(cx,h/2+.04,cz+depth/2),(w,h,.025),'Oak')
        for level in range(levels+1):
            y=.04+level*h/levels; box('ShelfBoard',(cx,y,cz),(w+.06,.05,depth+.02),'OakLight')
            if level==levels:continue
            x=cx-w/2+.06
            for k in range(9):
                bw=rng.uniform(.035,.075); bh=rng.uniform(.13,h/levels-.075)
                if x+bw>cx+w/2-.03:break
                cover=rng.choice(['BookBlue','BookCream','BookRose'])
                box('BookCover',(x,y+.03+bh/2,cz-.035),(bw,bh,depth*.65),cover,.003)
                box('BookPages',(x,y+.03+bh/2,cz-.035),(bw*.72,bh*.93,depth*.66),'Pages',.002)
                # The front spine remains colored.
                box('BookSpine',(x,y+.03+bh/2,cz-depth*.37),(bw,bh,.015),cover,.002)
                box('SpineBand',(x,y+.065,cz-depth*.38),(bw*.78,.009,.002),'Gold',0)
                x+=bw+.014
    shelf(1.62,.77,.88,.97,.38,3)
    shelf(-1.10,2.00,.65,1.72,.32,4)
    box('BedsideCabinet',(-.63,.29,1.38),(.40,.58,.39),'Oak')
    for y in (.14,.34,.52):
        box('DrawerFront',(-.63,y,1.175),(.35,.15,.02),'OakLight',.005)
        ball('DrawerKnob',(-.63,y,1.155),(.012,.012,.012),'Gold')
    # A lamp with a tapered, open shade. Actual point light is authored in Unity.
    ball('LampBase',(-.63,.61,1.38),(.10,.025,.10),'Gold')
    line('LampStem',[(-.63,.62,1.38),(-.63,.92,1.38)],.012,'Gold')
    verts=[]; faces=[]
    for j in range(2):
        for i in range(64):
            a=math.tau*i/64; r=.15 if j==0 else .105
            verts.append((-.63+math.cos(a)*r,.83+j*.24,1.38+math.sin(a)*r))
    for i in range(64):faces.append((i,(i+1)%64,(i+1)%64+64,i+64))
    mesh('Lampshade',verts,faces,'LampShade')
    # Plants: curved stems and shaped leaves, all real meshes.
    def leaf(p,angle,length=.10,width=.033):
        # Curved pointed leaf with a central fold and tapered silhouette, not a flattened ball.
        x,y,z=p; verts=[]; faces=[]; rows=12; cols=6
        for j in range(rows+1):
            t=j/rows; w=width*math.sin(math.pi*t)**.7
            for i in range(cols+1):
                side=(i/cols-.5)*2; along=length*t
                verts.append((x+math.cos(angle)*along-math.sin(angle)*side*w,
                              y+.028*math.sin(math.pi*t)-.009*abs(side)-.025*t,
                              z+math.sin(angle)*along+math.cos(angle)*side*w))
        for j in range(rows):
            for i in range(cols):
                a=j*(cols+1)+i;faces.append((a,a+cols+1,a+cols+2,a+1))
        mesh('CurvedLeaf',verts,faces,'Leaf')
        line('LeafMidrib',[(x+math.cos(angle)*length*t,y+.028*math.sin(math.pi*t)-.025*t+.0004,z+math.sin(angle)*length*t) for t in [i/16 for i in range(17)]],.0006,'Leaf')
    def plant(x,y,z,scale=1):
        verts=[]; faces=[]
        for j,(height,radius) in enumerate([(0,.065),(.12,.085),(.135,.091),(.14,.082),(.12,.075)]):
            for i in range(48):
                a=math.tau*i/48; verts.append((x+radius*math.cos(a),y+height,z+radius*math.sin(a)))
        for j in range(4):
            for i in range(48):
                a=j*48+i;b=j*48+(i+1)%48;faces.append((a,a+48,b+48,b))
        mesh('TerracottaPot_Rim',verts,faces,'Pot')
        ball('PlantSoil',(x,y+.115,z),(.074,.009,.074),'Oak')
        for branch in range(7):
            angle=branch*math.tau/7
            points=[(x+math.cos(angle)*.09*t,y+.12+.28*t,z+math.sin(angle)*.09*t) for t in [i/12 for i in range(13)]]
            line('PlantStem',points,.0015,'Leaf')
            for j in (4,7,10):leaf(points[j],angle+(.9 if j%2 else -.9),.10,.034)
    plant(1.00,.68,2.08); plant(1.79,1.02,.93)
    for branch in range(3):
        points=[]
        for i in range(65):
            t=i/64;points.append((1.33+.11*math.sin(t*5+branch),1.90-t*(.90+branch*.13),2.06-.09*math.sin(t*3)))
        line('TrailingVine',points,.0018,'Leaf')
        for i in range(3,63,5):leaf(points[i],-math.pi/2+(1 if i%2 else -1)*.9,.105,.035)
    # Rugs with lifted loop-like fibers, not a smooth rectangle.
    for cx,cz,rx,rz in [(-.23,-.57,.73,.48),(.30,1.18,.48,.31)]:
        ball('RugBacking',(cx,.008,cz),(rx,.014,rz),'Rug')
        verts=[]; faces=[]
        for i in range(int(rx*rz*100000)):
            a=rng.random()*math.tau; r=math.sqrt(rng.random())
            x=cx+rx*r*math.cos(a); z=cz+rz*r*math.sin(a); turn=rng.random()*math.tau
            base=len(verts); width=.0005; height=rng.uniform(.004,.007)
            for k in range(13):
                t=k/12; along=(t-.5)*.009; y=.019+math.sin(math.pi*t)*height
                px=x+math.cos(turn)*along; pz=z+math.sin(turn)*along
                verts.extend([(px-math.sin(turn)*width,y,pz+math.cos(turn)*width),(px+math.sin(turn)*width,y,pz-math.cos(turn)*width)])
            for k in range(12):
                b=base+k*2; faces.append((b,b+1,b+3,b+2))
        rug=mesh('RugLoopPile',verts,faces,'Rug')
        rug.data.normals_split_custom_set_from_vertices([(0,0,1)]*len(verts))
    # Five-point stars are room props for look development, not completed collection gameplay.
    for p,r in [((.34,.12,-.10),.085),((.65,.16,.55),.075),((.20,.15,1.35),.08),((1.52,1.12,.70),.11),((-.62,1.08,1.38),.035)]:star('LuminousStar',p,r)
    for i in range(11):
        x=-2.1+i*.20; y=1.8-.19*math.sin(i/10*math.pi)
        star('StringStar',(x,y,2.14),.035,.008)
    line('StarGarland',[(-2.1+i*.02,1.8-.19*math.sin(i/100*math.pi),2.14) for i in range(101)],.002,'Gold')
    # Hanging poster, original moon and star decoration.
    box('PosterFrame',(-1.85,1.44,2.205),(.48,.66,.025),'Trim',.008)
    box('PosterBlue',(-1.85,1.44,2.185),(.43,.61,.012),'BookBlue',.001)
    line('CrescentMoon',[(-1.86+.13*math.cos(.6+5.0*i/48),1.49+.13*math.sin(.6+5.0*i/48),2.17) for i in range(49)],.025,'Gold')
    # Original toy blocks in the foreground.
    for x,z in [(-.94,-.80),(-.82,-.62),(-1.02,-.56)]:
        box('ToyBlock',(x,.055,z),(.11,.11,.11),rng.choice(['BookBlue','BookCream','BookRose']),.009)
    # World-projected UVs before grouping, keep the editable objects in the source blend.
    for obj in objects:
        if obj==quilt:continue
        uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
        for loop in obj.data.loops:
            p=obj.matrix_world@obj.data.vertices[loop.vertex_index].co
            if obj.name.startswith('FloorBoard'):uv.data[loop.index].uv=(-p.y*5,p.x*.8)
            else:uv.data[loop.index].uv=(p.x*2,p.y*2) if abs(loop.normal.z)>.6 else (p.x*2,p.z*2)
    for name,p in [('RoomForward_UnityCheck',(0,0,1)),('RoomRight_UnityCheck',(1,0,0))]:
        o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=coord(p)
    scene['provenance']='Original procedural geometry, owner twilight references; no photos used as background or texture.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'TwilightRoom.blend'))
    # Export duplicates, grouped by material, without altering the editable source file.
    exports=[]
    for name in specs:
        group=[o for o in objects if o.data.materials[0].name==name]
        if not group:continue
        bpy.ops.object.select_all(action='DESELECT'); copies=[]
        for obj in group:
            o=obj.copy(); o.data=obj.data.copy(); scene.collection.objects.link(o); o.select_set(True); copies.append(o)
        bpy.context.view_layer.objects.active=copies[0]; bpy.ops.object.join(); obj=bpy.context.object; obj.name='Room_'+name; exports.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for o in exports+[o for o in scene.objects if o.name.endswith('_UnityCheck')]:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'TwilightRoom.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False)
    for obj in exports:obj.data.calc_loop_triangles()
    finish(OUTPUT,RECIPE,{'blender':bpy.app.version_string,'state':'generated_editable_room_trial','source_objects':len(objects),
        'export_meshes':len(exports),'triangles':sum(len(o.data.loop_triangles) for o in exports),'unity_import':'not_yet_verified','cloth_bake':scene['cloth_bake'],'cloth_min_height_m':scene['cloth_min_height_m'],
        'provenance':'original geometry guided by owner-selected twilight references; private reference rights unreviewed'})
    print('ROOM EXPORT COMPLETE',flush=True)

if __name__=='__main__':main()
