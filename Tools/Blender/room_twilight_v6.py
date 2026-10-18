"""Add tactile hero props to the protected v005 room. Dry-run unless --write.

Input is an actual editable .blend, not a flattened reference or panorama.
Unity coordinates in recipe: metres, Y up, +Z forward. Blender: (x,-z,y).
"""
import argparse
import json
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/Pipeline'))
from art_contract import preflight, finish, sha

BASE = ROOT/'Artifacts/SourceArt/Room/twilight_v005'
OUTPUT = ROOT/'Artifacts/SourceArt/Room/twilight_v006'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    base = json.loads((BASE/'receipt.json').read_text())
    for name, digest in base['files'].items():
        path = BASE/name
        if path.is_symlink() or sha(path) != digest:
            raise ValueError('Preserve owner edits in source: '+name)
    recipe = 'room-twilight-v006-'+sha(__file__)+sha(BASE/'receipt.json')
    reuse = preflight(OUTPUT, recipe)
    print('ROOM DETAIL PREFLIGHT', 'REUSE' if reuse else 'CREATE', flush=True)
    if reuse or not args.write:
        return
    import bpy
    from mathutils import Vector
    if bpy.app.version[:2] != (5, 2):
        raise RuntimeError('Reviewed Blender 5.2 required')
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'TwilightRoom.blend'))
    scene = bpy.context.scene
    OUTPUT.mkdir(parents=True, exist_ok=True)
    def coord(p):
        x, y, z = p
        return x, -z, y
    colors = {'Rattan': (.55,.36,.18), 'RattanShade': (.39,.25,.13),
              'KnitIce': (.48,.57,.66), 'KnitShade': (.39,.47,.57),
              'RugBlue': (.38,.48,.60), 'DistantPine': (.075,.10,.17)}
    for name, color in colors.items():
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*color, 1)
    added = []
    def mesh(name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata([coord(p) for p in vertices], [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        obj.data.materials.append(bpy.data.materials[material])
        for face in data.polygons:
            face.use_smooth = True
        added.append(obj)
        return obj
    def tube(name, points, radius, material):
        # Four-sided yarn cross-section, smooth normals; explicit bounded polygon cost.
        verts, faces = [], []
        points = [Vector(p) for p in points]
        for i, p in enumerate(points):
            tangent = (points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
            reference = Vector((0,1,0)) if abs(tangent.y)<.95 else Vector((1,0,0))
            side = tangent.cross(reference).normalized()
            normal = tangent.cross(side).normalized()
            for j in range(4):
                a = math.tau*j/4
                verts.append(tuple(p+radius*(side*math.cos(a)+normal*math.sin(a))))
            if i:
                for j in range(4):
                    a = (i-1)*4+j
                    faces.append((a,(i-1)*4+(j+1)%4,i*4+(j+1)%4,i*4+j))
        return mesh(name, verts, faces, material)

    # Open woven basket: crossing horizontal weft and vertical stakes, no solid side shell.
    cx, cz, height = 1.02, .20, .265
    stakes, rows = 48, 32
    for row in range(rows):
        y = .018+(row+.5)*height/rows
        t = y/height
        verts, faces = [], []
        for i in range(stakes*6+1):
            a = math.tau*i/(stakes*6)
            weave = .0022*math.cos(a*stakes/2+row*math.pi)
            for dy in (-.0031,.0031):
                verts.append((cx+(.165+.060*t+weave)*math.cos(a),y+dy,
                              cz+(.125+.035*t+weave)*math.sin(a)))
            if i:
                k=(i-1)*2
                faces.append((k,k+1,k+3,k+2))
        obj = mesh('Basket_InterlacedWeft',verts,faces,'Rattan' if row%3 else 'RattanShade')
        mod=obj.modifiers.new('SplitReedThickness','SOLIDIFY');mod.thickness=.0014
        bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    for stake in range(stakes):
        a=math.tau*stake/stakes
        points=[]
        for i in range(rows*3+1):
            t=i/(rows*3);wave=-.0022*math.cos(t*rows*math.pi+stake*math.pi)
            points.append((cx+(.165+.060*t+wave)*math.cos(a),.018+height*t,
                           cz+(.125+.035*t+wave)*math.sin(a)))
        tube('Basket_UprightStake',points,.0022,'Rattan')
    for dy in (0,.005,.010):
        tube('Basket_BoundRim',[(cx+.226*math.cos(i*math.tau/192),height+.018+dy,
                               cz+.161*math.sin(i*math.tau/192)) for i in range(193)],.0035,'Rattan')
    # Linen lining stops below the rim, retaining a visible open mouth.
    verts,faces=[],[]
    for j in range(12):
        t=j/11
        for i in range(96):
            a=math.tau*i/96;ripple=.0015*math.sin(a*24)
            verts.append((cx+(.162+.056*t+ripple)*math.cos(a),.020+.245*t,
                          cz+(.121+.033*t+ripple)*math.sin(a)))
    for j in range(11):
        for i in range(96):
            a=j*96+i;b=j*96+(i+1)%96
            faces.append((a,b,b+96,a+96))
    mesh('Basket_LinenLining',verts,faces,'IvoryTextile')
    # Woven base disk; basket remains hollow, with a real bottom.
    mesh('Basket_Base',[(cx,.021,cz)]+[(cx+.166*math.cos(i*math.tau/96),.021,cz+.126*math.sin(i*math.tau/96)) for i in range(96)],
         [(0,i+1,(i+1)%96+1) for i in range(96)],'RattanShade')

    # A knitted throw across the foot of the bed. Folded surface follows the mattress edge.
    def blanket(u,v,offset=0):
        x=-1.99+1.08*u
        if v<.40:
            t=v/.40; y=.23+.26*t; z=-.33
            n=Vector((0,0,-1))
        elif v<.56:
            t=(v-.40)/.16;a=t*math.pi/2
            y=.49+.06*math.sin(a);z=-.27-.06*math.cos(a)
            n=Vector((0,math.sin(a),-math.cos(a)))
        else:
            t=(v-.56)/.44;y=.55+.008*math.sin(math.pi*t);z=-.27+.44*t
            n=Vector((0,.44,-.008*math.pi*math.cos(math.pi*t))).normalized()
        y+=.005*math.sin(u*math.tau*5)*(1-v)*.4
        p=Vector((x,y,z))
        # Yarn offset follows the rounded front fold and the top surface.
        return tuple(p+n*offset)
    verts=[blanket(i/64,j/48) for j in range(49) for i in range(65)]
    faces=[]
    for j in range(48):
        for i in range(64):
            a=j*65+i;faces.append((a,a+65,a+66,a+1))
    backing=mesh('Throw_FoldedFabric',verts,faces,'KnitShade')
    mod=backing.modifiers.new('FabricThickness','SOLIDIFY');mod.thickness=.003
    bpy.context.view_layer.objects.active=backing;bpy.ops.object.modifier_apply(modifier=mod.name)
    for row in range(38):
        for col in range(60):
            points=[]
            for i in range(17):
                t=i/16
                # Interlocking rounded V stitches; two raised yarn legs per knit stitch.
                u=(col+.5+.42*math.sin(math.tau*t))/60
                v=(row+.18+.78*(1-math.cos(math.tau*t))*.5)/38
                points.append(blanket(u,v,.0024+.0012*math.sin(math.tau*t)))
            tube('Throw_KnitStitch',points,.00125,'KnitIce' if (row+col)%7 else 'KnitShade')
    for i in range(81):
        p=Vector(blanket(i/80,0))
        tube('Throw_TwistedFringe',[tuple(p+Vector((.002*math.sin(t*math.tau+i),-.035*t,-.003*t))) for t in [j/8 for j in range(9)]],.0014,'KnitIce')

    # Varied rug clusters and broad muted-blue rings, preserving the existing short loop geometry.
    for obj in list(scene.objects):
        if not obj.name.startswith('RugLoopPile'):
            continue
        obj.data.materials.append(bpy.data.materials['RugBlue'])
        center=(-.23,-.57,.73,.48) if obj.data.vertices[0].co.y>0 else (.30,1.18,.48,.31)
        rx,rz=center[2:]
        # Determine the rug from its bounding box, independent of one random strand.
        xs=[v.co.x for v in obj.data.vertices]; cxrug=(min(xs)+max(xs))*.5
        cxrug,czrug,rx,rz=(-.23,-.57,.73,.48) if cxrug<0 else (.30,1.18,.48,.31)
        for index in range(0,len(obj.data.vertices),26):
            p=obj.data.vertices[index].co
            r=math.sqrt(((p.x-cxrug)/rx)**2+((-p.y-czrug)/rz)**2)
            blue=.30<r<.44 or .70<r<.81
            clump=.80+.30*math.sin(p.x*47)*math.sin(p.y*53)
            for v in obj.data.vertices[index:index+26]:
                v.co.z=.019+(v.co.z-.019)*clump
            if blue:
                for face in obj.data.polygons[(index//26)*12:(index//26+1)*12]:face.material_index=1

    # Distant trees are real 3D geometry outside the room; no concept image backdrop.
    for tree,(x,z,h) in enumerate([(-1.6,5.8,1.7),(-.5,6.4,1.45),(1.3,5.3,1.9),(2.2,6.8,1.55)]):
        for tier in range(5):
            y=.10+h*tier*.14;radius=h*(.26-.033*tier)
            vertices=[(x,y+h*.41,z)]+[(x+radius*math.cos(i*math.tau/12),y+.025*math.sin(i*7),z+radius*math.sin(i*math.tau/12)) for i in range(12)]
            mesh('Exterior_Pine_Tier',vertices,[(0,i+1,(i+1)%12+1) for i in range(12)],'DistantPine')

    for obj in added:
        uv=obj.data.uv_layers.new(name='UVMap')
        for loop in obj.data.loops:
            p=obj.data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv=(p.x*2,p.y*2) if abs(loop.normal.z)>.6 else (p.x*2,p.z*2)
    scene['detail_provenance']='Original modeled basket, knit throw, rug pattern and exterior trees. No Tripo assets or downloaded textures.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'TwilightRoom.blend'))
    # Export copies grouped by primary material. Source objects and multi-material rugs stay editable.
    objects=[o for o in scene.objects if o.type=='MESH']
    exports=[]
    for name in sorted({o.data.materials[0].name for o in objects}):
        bpy.ops.object.select_all(action='DESELECT');copies=[]
        for obj in objects:
            if obj.data.materials[0].name!=name:continue
            copy=obj.copy();copy.data=obj.data.copy();scene.collection.objects.link(copy);copy.select_set(True);copies.append(copy)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
        obj=bpy.context.object;obj.name='Room_'+name;exports.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in exports+[o for o in scene.objects if o.name.endswith('_UnityCheck')]:obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'TwilightRoom.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False)
    for obj in exports:obj.data.calc_loop_triangles()
    finish(OUTPUT,recipe,{'blender':bpy.app.version_string,'state':'generated_editable_room_trial',
        'source_objects':len(objects),'export_meshes':len(exports),'triangles':sum(len(o.data.loop_triangles) for o in exports),
        'base_receipt_sha256':sha(BASE/'receipt.json'),'knit_stitches':38*60,'basket_stakes':stakes,'basket_rows':rows,
        'cloth_bake':scene['cloth_bake'],'cloth_min_height_m':scene['cloth_min_height_m'],
        'unity_import':'not_yet_verified','provenance':scene['detail_provenance']})
    print('ROOM DETAIL EXPORT COMPLETE',flush=True)


if __name__=='__main__':
    main()
