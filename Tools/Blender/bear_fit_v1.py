"""Broaden stance and soften belly from protected editable bear v006.
Dry-run by default. Original body/patches/pile and bone hierarchy remain editable.
"""
import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/Pipeline'))
sys.path.insert(0, str(ROOT/'Tools/Blender'))
from art_contract import preflight, finish, sha
from bear_fit_shape import point, array

BASE = ROOT/'Artifacts/SourceArt/Bear/twilight_v006'
OUTPUT = ROOT/'Artifacts/SourceArt/Bear/fit_v001'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    source = json.loads((BASE/'receipt.json').read_text())
    for name, digest in source['files'].items():
        if (BASE/name).is_symlink() or sha(BASE/name) != digest:
            raise ValueError('Protected source edited: '+name)
    recipe = 'bear-fit-v001-'+sha(__file__)+sha(ROOT/'Tools/Blender/bear_fit_shape.py')+sha(BASE/'receipt.json')
    reuse = preflight(OUTPUT, recipe)
    print('BEAR FIT PREFLIGHT', 'REUSE' if reuse else 'CREATE', flush=True)
    if reuse or not args.write:
        return
    import bpy
    import numpy as np
    from mathutils import Vector
    if bpy.app.version[:2] != (5, 2):
        raise RuntimeError('Reviewed Blender 5.2 required')
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'Sorajiro_Bear_M2.blend'))
    OUTPUT.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    rig = bpy.data.objects['SorajiroRig']
    meshes = [o for o in scene.objects if o.type == 'MESH']
    observations = {'base_receipt_sha256': sha(BASE/'receipt.json'), 'blender': bpy.app.version_string,
                    'units': 'metres, Blender Z-up/-Y face, FBX -Z forward/Y-up',
                    'state': 'generated_editable_shape_trial_not_approved'}
    for obj in meshes:
        data = obj.data
        values = np.empty(len(data.vertices)*3, dtype=np.float64)
        data.vertices.foreach_get('co', values)
        before = values.reshape(-1, 3)
        after = array(before, np)
        if not np.isfinite(after).all():
            raise RuntimeError('Non-finite shape rejected')
        if obj.name.startswith('FullBody_ShortPile'):
            # Preserve the authored fiber normals with the inverse-transpose deformation.
            loops = np.empty(len(data.loops), dtype=np.int32)
            data.loops.foreach_get('vertex_index', loops)
            corner = np.empty(len(data.loops)*3, dtype=np.float64)
            data.corner_normals.foreach_get('vector', corner)
            normal = np.empty_like(before)
            normal[loops] = corner.reshape(-1, 3)
            jac = np.empty((len(before), 3, 3), dtype=np.float64)
            for axis in range(3):
                offset = np.zeros(3); offset[axis] = .00001
                jac[:, :, axis] = (array(before+offset, np)-array(before-offset, np))/.00002
            if np.linalg.det(jac).min() <= 0:
                raise RuntimeError('Folded shape rejected')
            normal = np.linalg.solve(np.transpose(jac, (0,2,1)), normal[...,None])[...,0]
            normal /= np.linalg.norm(normal, axis=1)[:,None]
        data.vertices.foreach_set('co', after.ravel())
        data.update()
        if obj.name.startswith('FullBody_ShortPile'):
            data.normals_split_custom_set_from_vertices(normal.tolist())
        if obj.name == 'Bear_ContinuousCloth':
            def width(v, lo, hi):
                section = v[(v[:,2] > lo) & (v[:,2] < hi) & (abs(v[:,1]) < .04)]
                return float(np.ptp(section[:,0]))
            belly = (before[:,2] > .12) & (before[:,2] < .16) & (abs(before[:,0]) < .060)
            observations.update({'body_width_before_m': width(before, .12, .16), 'body_width_after_m': width(after,.12,.16),
                                 'belly_depth_before_m': float(np.ptp(before[belly,1])), 'belly_depth_after_m': float(np.ptp(after[belly,1])),
                                 'face_max_change_m': float(abs(after[before[:,2]>=.22]-before[before[:,2]>=.22]).max())})
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True); bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    foot_before = {}
    for bone in rig.data.edit_bones:
        if bone.name.startswith('foot_'): foot_before[bone.name] = tuple(bone.head)
        bone.head = Vector(point(*bone.head)); bone.tail = Vector(point(*bone.tail))
    observations['foot_centers_before_m'] = foot_before
    observations['foot_centers_after_m'] = {b.name: tuple(b.head) for b in rig.data.edit_bones if b.name.startswith('foot_')}
    bpy.ops.object.mode_set(mode='OBJECT')
    scene['shape_revision'] = 'Owner selects Fleece v005 variant 2. Belly wider/deeper, stance +8mm each side, face unchanged. Clips must be rebuilt from imported rest pose.'
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'Sorajiro_Bear_Fit.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    copies = []
    for obj in meshes:
        if obj.name.startswith('FullBody_ShortPile'): continue
        copy = obj.copy(); copy.data = obj.data.copy(); scene.collection.objects.link(copy); copy.select_set(True); copies.append(copy)
    bpy.context.view_layer.objects.active = copies[0]; bpy.ops.object.join()
    body = bpy.context.object; body.name = 'Bear_SkinnedBody_ArtTrial'
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig, body, *[o for o in meshes if o.name.startswith('FullBody_ShortPile')], *[o for o in scene.objects if o.name.endswith('_UnityCheck')]]:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'Sorajiro_Bear_Fit.fbx'), use_selection=True,
        object_types={'ARMATURE','MESH','EMPTY'}, axis_forward='-Z', axis_up='Y', global_scale=1,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
        bake_anim=False, use_mesh_modifiers=True, mesh_smooth_type='FACE', path_mode='AUTO')
    finish(OUTPUT, recipe, observations)
    print('BEAR FIT GENERATED', json.dumps(observations), flush=True)


if __name__ == '__main__': main()
