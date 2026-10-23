"""Refine editable v007 bedding. Dry-run by default; preserves all earlier revisions.
Unity metres/Y-up/+Z -> Blender (x,-z,y); FBX -Z forward/Y up.
"""
import argparse
import json
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/Pipeline'))
from art_contract import preflight, finish, sha
BASE = ROOT/'Artifacts/SourceArt/Room/twilight_v007'
OUTPUT = ROOT/'Artifacts/SourceArt/Room/twilight_v011'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    base = json.loads((BASE/'receipt.json').read_text())
    for name, digest in base['files'].items():
        if (BASE/name).is_symlink() or sha(BASE/name) != digest:
            raise ValueError('Source edited; preserve '+name)
    recipe = 'room-twilight-v011-'+sha(__file__)+sha(BASE/'receipt.json')
    reuse = preflight(OUTPUT, recipe)
    print('BEDDING PREFLIGHT', 'REUSE' if reuse else 'CREATE', flush=True)
    if reuse or not args.write:
        return
    import bpy
    import bmesh
    from mathutils import Vector
    if bpy.app.version[:2] != (5, 2):
        raise RuntimeError('Reviewed Blender 5.2 required')
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'TwilightRoom.blend'))
    scene = bpy.context.scene
    OUTPUT.mkdir(parents=True, exist_ok=True)
    removed = []
    for obj in list(scene.objects):
        if obj.name.startswith(('CompressedPillow', 'PillowPiping', 'Throw_', 'QuiltStitchedChannel')):
            removed.append(obj.name)
            bpy.data.objects.remove(obj, do_unlink=True)
    def coord(p):
        return p[0], -p[2], p[1]
    added = []
    def mesh(name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata([coord(p) for p in vertices], [], faces)
        data.update()
        bm = bmesh.new(); bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(data); bm.free()
        obj = bpy.data.objects.new(name, data); scene.collection.objects.link(obj)
        data.materials.append(bpy.data.materials[material])
        for face in data.polygons:
            face.use_smooth = True
        added.append(obj)
        return obj
    def tube(name, points, radius, material, sides=6):
        points = [Vector(p) for p in points]; vertices = []; faces = []
        for i, p in enumerate(points):
            tangent = (points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
            reference = Vector((0,1,0)) if abs(tangent.y)<.95 else Vector((1,0,0))
            side = tangent.cross(reference).normalized(); normal = tangent.cross(side).normalized()
            for j in range(sides):
                a = math.tau*j/sides
                vertices.append(tuple(p+radius*(side*math.cos(a)+normal*math.sin(a))))
            if i:
                for j in range(sides):
                    a = (i-1)*sides+j
                    faces.append((a,(i-1)*sides+(j+1)%sides,i*sides+(j+1)%sides,i*sides+j))
        faces.extend([tuple(reversed(range(sides))),tuple(range((len(points)-1)*sides,len(points)*sides))])
        return mesh(name,vertices,faces,material)

    # Closed, sewn cushion: two rectangular cloth panels, no coincident polar rings.
    nx, nz = 64, 36
    def pillow(u,v,top):
        edge = max(0,(1-u*u)*(1-v*v))
        x = -1.45+u*(.48-.032*v**8)
        z = 1.535+v*(.190-.022*u**8)
        seam = .612+.019*v+.003*math.sin(u*3+v)
        loft = (.082 if top else -.034)*edge**.52
        compression = -.012*math.exp(-((u-.23)/.38)**2-((v+.10)/.52)**2)*edge if top else 0
        wrinkle = .0032*math.sin(u*44+v*16)*math.exp(-((abs(v)-.83)/.11)**2)*edge**.25
        wrinkle += .0023*math.sin(v*37-u*12)*math.exp(-((abs(u)-.86)/.09)**2)*edge**.25
        return x,seam+loft+compression+wrinkle+(.003 if top else -.003),z
    vertices = [pillow(-1+2*i/nx,-1+2*j/nz,top) for top in (True,False) for j in range(nz+1) for i in range(nx+1)]
    count = (nx+1)*(nz+1); faces=[]
    for layer in range(2):
        for j in range(nz):
            for i in range(nx):
                a=layer*count+j*(nx+1)+i
                faces.append((a,a+1,a+nx+2,a+nx+1) if layer else (a,a+nx+1,a+nx+2,a+1))
    border = list(range(nx+1))+[j*(nx+1)+nx for j in range(1,nz+1)]+list(range(nz*(nx+1)+nx-1,nz*(nx+1)-1,-1))+[j*(nx+1) for j in range(nz-1,0,-1)]
    for a,b in zip(border,border[1:]+border[:1]):
        faces.append((a,b,b+count,a+count))
    cushion=mesh('Pillow_TwoSewnPanels',vertices,faces,'IvoryTextile')
    topology=bmesh.new();topology.from_mesh(cushion.data)
    if any(not edge.is_manifold for edge in topology.edges):raise RuntimeError('Pillow must be a closed sewn surface')
    topology.free()
    seam=[]
    for index in border+border[:1]:
        a=Vector(vertices[index]);b=Vector(vertices[index+count]);seam.append(tuple((a+b)*.5))
    tube('Pillow_SoftRolledSeam',seam,.0018,'IvoryTextile',8)
    # Fine quilting stitches sit on the old real-Cloth surface; the gravity drape is retained.
    quilt=bpy.data.objects['Quilt_GRAVITY_DRAPED']
    # The protected source stores a subdivided surface and UVs. Sample seam paths from its UVs.
    uv=quilt.data.uv_layers.active
    lookup={}
    for loop in quilt.data.loops:
        key=loop.vertex_index
        lookup.setdefault(key,tuple(uv.data[loop.index].uv))
    for axis,lines in ((0,6),(1,8)):
        for k in range(1,lines):
            target=k/lines
            candidates=[(pair[1-axis],quilt.data.vertices[index]) for index,pair in lookup.items() if abs(pair[axis]-target)<.001 and quilt.data.vertices[index].normal.z>-.8]
            # Use the highest point at each UV coordinate, avoiding the underside of Solidify.
            points={}
            for t,vertex in candidates:
                key=round(t,5)
                if key not in points or vertex.co.z>points[key].co.z:points[key]=vertex
            path=[]
            for t,vertex in sorted(points.items()):
                p=vertex.co+vertex.normal*.00035
                path.append((p.x,p.z,-p.y))
            if len(path)>5:tube('Quilt_RecessedToneSeam',path,.00032,'BlueTextile',4)
    # Rounded stockinette yarn follows a softly rumpled throw over a continuous rounded fold.
    from mathutils.bvhtree import BVHTree
    surface=BVHTree.FromPolygons([v.co.copy() for v in quilt.data.vertices],[tuple(p.vertices) for p in quilt.data.polygons])
    def quilt_height(x,z):
        point,normal,index,distance=surface.ray_cast(Vector((x,-z,2)),Vector((0,0,-1)),3)
        if point is None:
            point,normal,index,distance=surface.find_nearest(Vector((x,-z,.56)))
        if point is None:raise RuntimeError('Throw must contact protected drape')
        return point.z
    def blanket(u,v,offset=0):
        x=-2.015+1.105*u+.008*math.sin(v*4+u*2)
        top=quilt_height(x,-.10)+.003
        if v<.40:
            t=v/.40;y=.235+(top-.237-.235)*t;z=-.337;n=Vector((0,0,-1))
        elif v<.56:
            a=(v-.40)/.16*math.pi/2;y=top-.237+.237*math.sin(a);z=-.10-.237*math.cos(a);n=Vector((0,math.sin(a),-math.cos(a)))
        else:
            t=(v-.56)/.44;z=-.10+.38*t;y=quilt_height(x,z)+.003;n=Vector((0,1,0))
        rumple=.002*math.sin(u*math.tau*3.4+v*2)+.001*math.sin(u*math.tau*7-v*5)
        p=Vector((x,y,z))
        # Shoot from outside the cloth along the expected surface normal. This follows the
        # real drape at the front as well as the top, retaining a free hanging tail below it.
        outward=Vector((n.x,-n.z,n.y))
        q,normal,index,distance=surface.ray_cast(Vector((p.x,-p.z,p.y))+outward, -outward,2)
        if q is not None:
            p=Vector((q.x,q.z,-q.y))
            n=Vector((normal.x,normal.z,-normal.y)).normalized()
            if n.dot(Vector((0,1,0)) if v>=.56 else Vector((0,0,-1)))<-.01:
                n=-n
            p+=n*.006
        elif v>=.56:raise RuntimeError('Top yarn must project onto actual quilt')
        return tuple(p+n*(rumple+offset))
    # Guard the actual fold used by every vertex and yarn strand against a visible split.
    max_join_gap=max((Vector(blanket(i/60,v-1e-6))-Vector(blanket(i/60,v+1e-6))).length for i in range(61) for v in (.4,.56))
    if max_join_gap>.001:raise RuntimeError('Fold discontinuity exceeds 1 mm: '+str(max_join_gap))
    vertices=[blanket(i/72,j/64) for j in range(65) for i in range(73)]
    faces=[(j*73+i,j*73+i+73,j*73+i+74,j*73+i+1) for j in range(64) for i in range(72)]
    backing=mesh('Throw_SoftContinuousBacking',vertices,faces,'KnitShade')
    mod=backing.modifiers.new('ClothBackingThickness','SOLIDIFY');mod.thickness=.003;mod.offset=-1
    bpy.context.view_layer.objects.active=backing;bpy.ops.object.modifier_apply(modifier=mod.name)
    columns, rows = 60, 48
    for row in range(rows):
        for col in range(columns):
            points=[]
            for i in range(21):
                t=i/20
                u=(col+.5+.40*math.sin(math.tau*t))/columns
                v=(row+.06+.96*(.5-.5*math.cos(math.tau*t)))/rows
                # Alternating under/over depth makes round interlocking legs rather than flat facets.
                points.append(blanket(u,v,.0035+.0020*math.sin(math.tau*t)))
            tube('Throw_RoundStockinette',points,.0021*(1+.05*math.sin(col*3+row*5)), 'KnitIce' if (row+col)%11 else 'KnitShade')
    for u in (0,1):tube('Throw_Selvedge',[blanket(u,j/128,.002) for j in range(129)],.0024,'KnitIce',8)
    for v in (0,1):tube('Throw_BoundEdge',[blanket(i/128,v,.002) for i in range(129)],.0024,'KnitIce',8)
    for i in range(61):
        p=Vector(blanket(i/60,0,.002))
        tube('Throw_SoftFringe',[tuple(p+Vector((.0014*math.sin(t*9+i),-.027*t,-.004*t))) for t in [j/10 for j in range(11)]],.0013,'KnitIce')
    for obj in added:
        if obj.data.uv_layers:continue
        uv=obj.data.uv_layers.new(name='UVMap')
        for loop in obj.data.loops:
            p=obj.data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv=(p.x*2,p.y*2) if abs(loop.normal.z)>.6 else (p.x*2,p.z*2)
    scene['bedding_revision']='v011: exterior surface-projected throw, continuous fold, cached BVH; sewn closed cushion, tone-on-tone quilt seam, rounded 6-sided stockinette with irregular folds. Original v005 simulated drape retained, no new cloth simulation.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'TwilightRoom.blend'))
    objects=[o for o in scene.objects if o.type=='MESH'];exports=[]
    for name in sorted({o.data.materials[0].name for o in objects}):
        bpy.ops.object.select_all(action='DESELECT');copies=[]
        for obj in objects:
            if obj.data.materials[0].name!=name:continue
            copy=obj.copy();copy.data=obj.data.copy();scene.collection.objects.link(copy);copy.select_set(True);copies.append(copy)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='Room_'+name;exports.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in exports+[o for o in scene.objects if o.name.endswith('_UnityCheck')]:obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'TwilightRoom.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False)
    for obj in exports:obj.data.calc_loop_triangles()
    finish(OUTPUT,recipe,{'blender':bpy.app.version_string,'state':'generated_editable_bedding_trial','base_receipt_sha256':sha(BASE/'receipt.json'),
        'source_objects':len(objects),'export_meshes':len(exports),'triangles':sum(len(o.data.loop_triangles) for o in exports),
        'removed_objects':len(removed),'knit_stitches':columns*rows,'pillow_closed_panels':True,'fold_join_max_gap_m':max_join_gap,
        'cloth_bake':scene['cloth_bake'],'provenance':scene['bedding_revision'],'unity_import':'not_yet_verified'})
    print('BEDDING EXPORT COMPLETE',flush=True)


if __name__=='__main__':main()
