"""Run with actual Blender --background --factory-startup --python ... -- [--write].
Editable mesh/UV/rig trial. No downloaded models or photo projection. Not approved art.
"""
import argparse
import json
import math
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/Pipeline'))
from art_contract import sha, preflight, finish

OUTPUT = ROOT / 'Artifacts/SourceArt/Bear/twilight_v001'
RECIPE = 'bear-twilight-v001-' + sha(__file__)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    reuse = preflight(OUTPUT, RECIPE)
    print('SORAJIRO ART PREFLIGHT:', 'REUSE' if reuse else 'CREATE', OUTPUT)
    if reuse or not args.write: return
    import bpy
    import numpy as np
    from mathutils import Vector
    if bpy.app.version[:2] != (5, 2): raise RuntimeError('Review Blender version before generating')
    OUTPUT.mkdir(parents=True, exist_ok=True)
    # Factory-startup is a private background process, not the owner's open Blender file.
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    materials = {}
    for name, color, roughness in [('BlueFleece',(.24,.57,.79,1),.86), ('WhiteApplique',(.89,.88,.84,1),.9),
                                  ('SafetyPlastic',(.006,.008,.011,1),.13),('Stitch',(.15,.34,.46,1),.9),
                                  ('PileBlue',(.24,.57,.79,1),1),('PileWhite',(.89,.88,.84,1),1)]:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = color; mat.use_nodes = True
        p = mat.node_tree.nodes.get('Principled BSDF')
        p.inputs['Base Color'].default_value = color; p.inputs['Roughness'].default_value = roughness
        materials[name] = mat

    def mesh(name, verts, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata(verts, [], faces); data.update()
        uv = data.uv_layers.new(name='UVMap')
        for loop in data.loops:
            v = data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv = ((v.x+.2)/.4, v.z/.4)
        obj = bpy.data.objects.new(name, data); scene.collection.objects.link(obj)
        obj.data.materials.append(materials[material])
        for poly in data.polygons: poly.use_smooth = True
        return obj

    def rounded(name, center, radii, exponent=1, tilt=0, material='BlueFleece'):
        # Superellipsoid, with rounded squareness and tapered limbs rather than stock spheres.
        verts=[]; faces=[]; rings=32; sides=64
        def power(v): return math.copysign(abs(v)**exponent, v)
        for j in range(rings+1):
            lat=-math.pi/2+math.pi*j/rings
            for i in range(sides):
                t=2*math.pi*i/sides
                x=radii[0]*power(math.cos(lat))*power(math.cos(t))
                y=radii[1]*power(math.cos(lat))*power(math.sin(t))
                z=radii[2]*power(math.sin(lat))
                x,z=x*math.cos(tilt)+z*math.sin(tilt), -x*math.sin(tilt)+z*math.cos(tilt)
                verts.append((center[0]+x,center[1]+y,center[2]+z))
        for j in range(rings):
            for i in range(sides):
                a=j*sides+i; b=j*sides+(i+1)%sides
                faces.append((a,b,b+sides,a+sides))
        return mesh(name, verts, faces, material)

    blue = [rounded('Head',(0,0,.285),(.100,.080,.084),.94),
            rounded('PearBody',(0,.003,.146),(.084,.060,.092),.96)]
    for s in (-1,1):
        blue.append(rounded('Ear', (s*.080,0,.365),(.032,.021,.032),1))
        blue.append(rounded('Arm', (s*.092,-.003,.164),(.025,.027,.060),1,s*-.43))
        blue.append(rounded('Foot',(s*.037,-.014,.041),(.031,.040,.041),1))
    blue.append(rounded('Tail',(0,.062,.085),(.016,.016,.016)))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in blue: obj.select_set(True)
    bpy.context.view_layer.objects.active=blue[0]
    bpy.ops.object.join()
    cloth=bpy.context.object; cloth.name='Bear_ContinuousCloth'
    remesh=cloth.modifiers.new('UnionSculpt','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.0017
    remesh.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=cloth.modifiers.new('SoftStuffing','SMOOTH'); smooth.factor=1; smooth.iterations=5
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    dec=cloth.modifiers.new('TrialTopology','DECIMATE'); dec.ratio=.48
    bpy.ops.object.modifier_apply(modifier=dec.name)
    for poly in cloth.data.polygons: poly.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.012)
    bpy.ops.object.mode_set(mode='OBJECT')
    cloth.data.update()

    def surface(x,z,side=-1):
        ok, p, n, _ = cloth.ray_cast(Vector((x,side*.5,z)),Vector((0,-side,0)))
        return (p,n) if ok else (None,None)

    patches=[]
    def patch(name,cx,cz,sx,sz,side=-1,shape='cloud',depth=.0004):
        # Raised stitched appliqué follows the actual sculpt surface, including side cheeks.
        boundary=[]
        if shape=='cloud':
            controls=[(-1,-.32),(-.99,.06),(-.82,.27),(-.57,.25),(-.52,.62),(-.22,.80),
                      (.1,.60),(.32,.43),(.52,.55),(.80,.41),(1,.13),(.96,-.31),(.54,-.43),(-.64,-.43)]
            for i in range(len(controls)):
                p0=Vector(controls[(i-1)%len(controls)]); p1=Vector(controls[i]); p2=Vector(controls[(i+1)%len(controls)]); p3=Vector(controls[(i+2)%len(controls)])
                for k in range(4):
                    t=k/4
                    boundary.append(tuple(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)))
        else: boundary=[(math.cos(i*2*math.pi/16),math.sin(i*2*math.pi/16)) for i in range(16)]
        verts=[]; count=len(boundary); rings=6
        for j in range(rings+1):
            r=max(.0001,j/rings)
            for bx,bz in boundary:
                p,n=surface(cx+sx*bx*r,cz+sz*bz*r,side)
                if p is None: return
                p+=n*(.00025+depth*math.sin(math.pi/2*(1-r))**.5)
                verts.append(tuple(p))
        faces=[]
        for j in range(rings):
            for i in range(count):
                a=j*count+i; b=j*count+(i+1)%count
                face=(a,b,b+count,a+count)
                faces.append(face if side==-1 else tuple(reversed(face)))
        p,n=surface(cx,cz,side)
        a,b,c=[Vector(verts[index]) for index in faces[-1][:3]]
        if (b-a).cross(c-a).dot(n)<0: faces=[tuple(reversed(face)) for face in faces]
        patches.append(mesh(name,verts,faces,'WhiteApplique'))

    for side in (-1,1):
        patch('Cloud_Head',.038,.330,.035,.023,side)
        patch('Cloud_CheekL',-.067,.248,.024,.021,side)
        patch('Cloud_CheekR',.070,.241,.020,.018,side)
        patch('Cloud_BodyHigh',.030,.179,.027,.021,side)
        patch('Cloud_BodyLow',-.020,.115,.034,.025,side)
        patch('Cloud_BodySide',.048,.094,.018,.017,side)
        rng=random.Random(607+side)
        for i in range(53):
            z=rng.uniform(.06,.35); x=rng.uniform(-.087,.087)
            # Keep safety eyes, muzzle, and primary clouds readable.
            if z>.315 and x>0: continue
            if side==-1 and .21<z<.315 and abs(x)<.074: continue
            if .14<z<.19 and x>.01: continue
            if z<.12 and x<.025: continue
            patch('WhiteDot',x,z,.0014,.0015,side,'dot',.0001)
    accessories=[]
    for s in (-1,1):
        accessories.append(rounded('WhiteInnerEar',(s*.080,-.020,.365),(.021,.005,.022),1,material='WhiteApplique'))
        p,n=surface(s*.046,.285)
        accessories.append(rounded('GlossSafetyEye',(s*.046,p.y-.004,.285),(.010,.006,.011),1,material='SafetyPlastic'))
    accessories.append(rounded('Muzzle',(0,-.079,.254),(.044,.022,.033),1,material='WhiteApplique'))
    accessories.append(rounded('GlossSafetyNose',(0,-.103,.270),(.013,.006,.009),.91,material='SafetyPlastic'))

    def tube(name,points,radius,material):
        data=bpy.data.curves.new(name,'CURVE'); data.dimensions='3D'; data.bevel_depth=radius; data.bevel_resolution=2
        spline=data.splines.new('POLY'); spline.points.add(len(points)-1)
        for p,v in zip(spline.points,points): p.co=(*v,1)
        obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.data.materials.append(materials[material])
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
        bpy.ops.object.convert(target='MESH'); return bpy.context.object
    def mouth_point(x,z):
        y=-.079-.022*math.sqrt(max(.01,1-(x/.044)**2-((z-.254)/.033)**2))-.001
        return (x,y,z)
    accessories.append(tube('EmbroideredSmile',[mouth_point(-.013+.026*i/24,.242+.004*((i-12)/12)**2) for i in range(25)],.00065,'SafetyPlastic'))
    accessories.append(tube('MouthStem',[mouth_point(0,.264-.022*i/12) for i in range(13)],.0006,'SafetyPlastic'))
    for s in (-1,1):
        points=[]
        for i in range(40):
            z=.097+i*.112/39; x=s*(.112-.016*(z-.097)/.112)
            p,n=surface(x,z)
            if p is not None: points.append(tuple(p+n*.0006))
        if len(points)>2: accessories.append(tube('ArmSeam',points,.0004,'Stitch'))

    # Distributed, curved opaque micro-ribbons. Native mesh experiment; no hair plugin.
    from mathutils.bvhtree import BVHTree
    white_objects=[*patches,*[o for o in accessories if o.data.materials[0].name=='WhiteApplique']]
    wverts=[]; wfaces=[]
    for obj in white_objects:
        base=len(wverts); wverts.extend(v.co.copy() for v in obj.data.vertices)
        wfaces.extend(tuple(base+i for i in p.vertices) for p in obj.data.polygons)
    white_bvh=BVHTree.FromPolygons(wverts,wfaces)
    rng=np.random.default_rng(412); verts=[]; faces=[]; normals=[]; matids=[]
    for obj in [cloth,*white_objects]:
        obj.data.calc_loop_triangles()
        tris=list(obj.data.loop_triangles)
        if not tris: continue
        areas=np.asarray([t.area for t in tris]); count=max(8,int(areas.sum()*680000))
        chosen=rng.choice(len(tris),count,p=areas/areas.sum())
        for index in chosen:
            tri=tris[index]; a,b,c=[obj.data.vertices[i] for i in tri.vertices]
            u,v=rng.random(2)
            if u+v>1: u,v=1-u,1-v
            p=a.co+(b.co-a.co)*u+(c.co-a.co)*v
            n=(a.normal*(1-u-v)+b.normal*u+c.normal*v).normalized()
            if obj==cloth and white_bvh.find_nearest(p,.0035)[0] is not None: continue
            tangent=n.cross(Vector((0,0,1)))
            if tangent.length<.001: tangent=n.cross(Vector((1,0,0)))
            tangent.normalize(); second=n.cross(tangent).normalized()
            angle=rng.uniform(0,math.tau); sidevec=tangent*math.cos(angle)+second*math.sin(angle)
            length=rng.uniform(.0012,.0027); width=rng.uniform(.00007,.00016)
            lean=second*(-length*.35)+tangent*rng.uniform(-.0006,.0006)
            base=len(verts)
            for j in range(3):
                t=j/2; center=p+n*(length*t+.0001)+lean*t*t
                verts.extend([tuple(center-sidevec*width*(1-t*.92)),tuple(center+sidevec*width*(1-t*.92))])
                normals.extend([tuple(n),tuple(n)])
            faces.extend([(base,base+1,base+3,base+2),(base+2,base+3,base+5,base+4)])
            matids.extend([0 if obj==cloth else 1]*2)
    fur=mesh('FullBody_ShortPile_TRIAL',verts,faces,'PileBlue')
    fur.data.materials.append(materials['PileWhite'])
    for poly,mid in zip(fur.data.polygons,matids): poly.material_index=mid
    fur.data.normals_split_custom_set_from_vertices(normals)
    print('FULL BODY PILE:',len(faces)//2,'ribbons',flush=True)

    # Editable full-body generic armature, with deterministic preview skin weights.
    armdata=bpy.data.armatures.new('SorajiroRig'); rig=bpy.data.objects.new('SorajiroRig',armdata); scene.collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    definitions={'root':((0,0,0),(0,0,.045),None),'body':((0,0,.07),(0,0,.22),'root'),
                 'head':((0,0,.22),(0,0,.34),'body')}
    for s,suffix in [(-1,'L'),(1,'R')]:
        definitions['arm_'+suffix]=((s*.075,0,.215),(s*.118,0,.105),'body')
        definitions['leg_'+suffix]=((s*.043,0,.09),(s*.043,-.008,.02),'body')
        definitions['ear_'+suffix]=((s*.077,0,.34),(s*.088,0,.39),'head')
    for name,(head,tail,parent) in definitions.items():
        bone=armdata.edit_bones.new(name); bone.head=head; bone.tail=tail
        if parent: bone.parent=armdata.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    def weights(obj, rigid=None):
        groups={name:obj.vertex_groups.new(name=name) for name in definitions}
        buckets={}
        for v in obj.data.vertices:
            x,y,z=v.co; selected={}
            if rigid: selected={rigid:1}
            else:
                headw=max(0,min(1,(z-.211)/.027)); side='R' if x>0 else 'L'
                armw=max(0,min(.95,(abs(x)-.064)/.035))*max(0,min(1,(.247-z)/.026)) if z>.079 else 0
                legw=max(0,min(1,(.094-z)/.03)) if abs(x)>.012 else 0
                selected={'head':headw,'body':(1-headw)*(1-armw)*(1-legw),
                          'arm_'+side:(1-headw)*armw,'leg_'+side:(1-headw)*(1-armw)*legw}
            total=sum(selected.values())
            for bone,weight in selected.items():
                if weight>0:
                    quantized=round(weight/total*32)/32
                    if quantized: buckets.setdefault((bone,quantized),[]).append(v.index)
        for (bone,weight),indices in buckets.items(): groups[bone].add(indices,weight,'REPLACE')
        modifier=obj.modifiers.new('Skin','ARMATURE'); modifier.object=rig; obj.parent=rig
    # Consolidate matching cloth details, retaining editable objects in the saved .blend.
    for obj in [cloth,*patches,*accessories,fur]: weights(obj,'head' if any(t in obj.name for t in ('Eye','Nose','Muzzle','Smile','Stem','InnerEar')) else None)
    rig['state']='generated_lookdev_trial_not_approved'; rig['forward']='Blender -Y, export -Z/Y, verify Unity +Z'
    for name,point in [('FaceForward_UnityCheck',(0,-.1,.25)),('Top_UnityCheck',(0,0,.4))]:
        obj=bpy.data.objects.new(name,None); scene.collection.objects.link(obj); obj.location=point; obj.parent=rig

    # Small deterministic tangent-space micro-pile texture, not a substitute for silhouette fur.
    n=512; rng=np.random.default_rng(607)
    height=rng.random((n,n)).astype(np.float32)
    for _ in range(2): height=(height+np.roll(height,1,0)+np.roll(height,-1,0)+np.roll(height,1,1)+np.roll(height,-1,1))/5
    dx=(np.roll(height,-1,1)-np.roll(height,1,1))*1.5
    dy=(np.roll(height,-1,0)-np.roll(height,1,0))*1.5
    normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1); normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    pixels=np.ones((n,n,4),dtype=np.float32); pixels[:,:,:3]=normal*.5+.5
    image=bpy.data.images.new('FleeceMicroNormal',width=n,height=n,alpha=True)
    image.colorspace_settings.name='Non-Color'; image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw=str(OUTPUT/'FleeceMicroNormal.png'); image.file_format='PNG'; image.save()
    # Diagnostic ortho cameras remain in source only; no offline render is presented as Unity evidence.
    scene['asset_provenance']='Original geometry; owner bear and twilight concept references; redistribution rights unreviewed.'
    scene['fur']='Full body 1.2-2.7mm micro-ribbon mesh trial. Not quality or motion approved.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'Sorajiro_Bear_M2.blend'))
    # Keep editable component objects in .blend, consolidate export into one skinned body.
    bpy.ops.object.select_all(action='DESELECT')
    duplicates=[]
    for obj in [cloth,*patches,*accessories]:
        duplicate=obj.copy(); duplicate.data=obj.data.copy(); scene.collection.objects.link(duplicate)
        duplicate.select_set(True); duplicates.append(duplicate)
    bpy.context.view_layer.objects.active=duplicates[0]
    bpy.ops.object.join()
    export_body=bpy.context.object; export_body.name='Bear_SkinnedBody_ArtTrial'
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig,export_body,fur,*[o for o in scene.objects if o.name.endswith('_UnityCheck')]]: obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'Sorajiro_Bear_M2.fbx'),use_selection=True,
        object_types={'ARMATURE','MESH','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,
        bake_anim=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',path_mode='AUTO')
    observations={'blender':bpy.app.version_string,'units':'metres / Z-up / -Y face; FBX -Z forward / Y-up',
        'height_m':max(v.co.z for v in cloth.data.vertices)-min(v.co.z for v in cloth.data.vertices),
        'cloth_vertices':len(cloth.data.vertices),'cloth_polygons':len(cloth.data.polygons),
        'bone_names':list(definitions),'fur_quads':len(fur.data.polygons),'fur_ribbons':len(fur.data.polygons)//2,
        'state':'generated_editable_model_uv_generic_rig_trial','animation':'not_created','unity_import':'not_yet_verified'}
    finish(OUTPUT,RECIPE,observations)
    print('SORAJIRO ART GENERATED:',json.dumps(observations))

if __name__=='__main__': main()
