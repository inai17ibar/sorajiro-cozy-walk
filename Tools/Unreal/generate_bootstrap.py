"""Run ONLY in the real UE GUI Editor. Default is preflight, never overwrite assets."""
from __future__ import annotations

import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools' / 'Pipeline'))
from recipe_contract import content_directory, digest_recipe, generation_action, snapshot, validate


def require(value, message):
    if not value:
        raise RuntimeError(message)
    return value


def run():
    import unreal  # There is deliberately no fake/stub Unreal module.
    mode = os.environ.get('SORAJIRO_GENERATE_MODE', 'preflight')
    require(mode in ('preflight', 'apply'), 'Mode must be preflight or apply')
    recipe = validate(json.loads((ROOT / 'Tools/Unreal/recipes/bootstrap_v001.json').read_text()))
    project = ROOT / 'Unreal'
    actual = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    require(actual == project.resolve(), 'Open this repository\'s Sorajiro.uproject first')
    # Reject even parent symlinks before constructing any binary assets.
    output = content_directory(project, recipe)
    require(output.resolve().is_relative_to(project.resolve() / 'Content'), 'Output escapes Content')
    for parent in [output, *output.parents]:
        if parent == project: break
        require(not parent.is_symlink(), 'Symlink in content destination')
    report_dir = ROOT / 'Artifacts/AssetGeneration'
    receipt_path = report_dir / (output.name + '.receipt.json')
    receipt = json.loads(receipt_path.read_text()) if receipt_path.exists() else None
    version = unreal.SystemLibrary.get_engine_version()

    for api in ('LevelEditorSubsystem', 'EditorActorSubsystem', 'EditorAssetSubsystem',
                'EditorLoadingAndSavingUtils', 'MaterialEditingLibrary', 'MaterialFactoryNew'):
        require(hasattr(unreal, api), 'Required editor API/plugin missing: ' + api)
    levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    require(not levels.is_in_play_in_editor(), 'Stop PIE before generation')
    require(not unreal.EditorLoadingAndSavingUtils.get_dirty_map_packages(),
            'Save your current map explicitly before generation')
    require(not unreal.EditorLoadingAndSavingUtils.get_dirty_content_packages(),
            'Save your edited content explicitly before generation')
    cube = require(assets.load_asset('/Engine/BasicShapes/Cube'), 'Engine cube unavailable')
    require(unreal.load_class(None, '/Script/Sorajiro.SorajiroGameMode'), 'Build native runtime module first')
    action = generation_action(recipe, output, receipt, version)
    map_path = recipe['output_root'] + '/' + recipe['map_name']
    # Protect registered assets as well as filesystem content (including unsaved packages).
    registered = list(assets.list_assets(recipe['output_root'], recursive=True, include_folder=False))
    if action == 'CREATE':
        require(not registered, 'Existing registered assets; use a fresh output revision')
    else:
        require(assets.load_asset(map_path), 'Existing map cannot be loaded')
        for item in recipe['materials']:
            require(assets.load_asset(recipe['output_root'] + '/' + item['name']), 'Material cannot be loaded')
    unreal.log('SORAJIRO preflight: ' + json.dumps({
        'mode': mode, 'action': action, 'map': map_path, 'engine': version,
        'recipe_sha256': digest_recipe(recipe), 'state': 'placeholder',
        'render_verified': False}))
    if mode == 'preflight' or action == 'REUSE':
        return  # No saves, no map switch, no actor duplication on a rerun.

    # Source recipe changes require a fresh revision. Never update an existing asset.
    tools = unreal.AssetToolsHelpers.get_asset_tools()
    materials = {}
    for item in recipe['materials']:
        material = require(tools.create_asset(item['name'], recipe['output_root'],
                           unreal.Material, unreal.MaterialFactoryNew()), 'Material creation failed')
        color = require(unreal.MaterialEditingLibrary.create_material_expression(
            material, unreal.MaterialExpressionConstant3Vector), 'Color expression failed')
        color.set_editor_property('constant', unreal.LinearColor(*item['linear_color'], 1.0))
        rough = require(unreal.MaterialEditingLibrary.create_material_expression(
            material, unreal.MaterialExpressionConstant), 'Roughness expression failed')
        rough.set_editor_property('r', item['roughness'])
        require(unreal.MaterialEditingLibrary.connect_material_property(
            color, '', unreal.MaterialProperty.MP_BASE_COLOR), 'Base color connection failed')
        require(unreal.MaterialEditingLibrary.connect_material_property(
            rough, '', unreal.MaterialProperty.MP_ROUGHNESS), 'Roughness connection failed')
        unreal.MaterialEditingLibrary.recompile_material(material)
        require(assets.save_loaded_asset(material, only_if_is_dirty=False), 'Material save failed')
        materials[item['name']] = material

    require(levels.new_level(map_path), 'Blank map creation failed')
    classes = {'cube': unreal.StaticMeshActor, 'player_start': unreal.PlayerStart,
               'directional_light': unreal.DirectionalLight, 'point_light': unreal.PointLight}
    for item in recipe['actors']:
        # Rotator positional order is pitch, yaw, roll.
        actor = require(actors.spawn_actor_from_class(classes[item['kind']],
            unreal.Vector(*item['location']), unreal.Rotator(*item.get('rotation', [0, 0, 0]))),
            'Spawn failed: ' + item['id'])
        actor.set_actor_label(item['id'])
        actor.set_editor_property('tags', ['SorajiroGenerator_m1_1', item['id']])
        if item['kind'] == 'cube':
            component = actor.get_component_by_class(unreal.StaticMeshComponent)
            component.set_static_mesh(cube)
            component.set_material(0, materials[item['material']])
            component.set_collision_profile_name('BlockAll')
            actor.set_actor_scale3d(unreal.Vector(*item['scale']))
        elif item['kind'].endswith('_light'):
            component = actor.get_component_by_class(unreal.LightComponent)
            component.set_mobility(unreal.ComponentMobility.MOVABLE)
            component.set_editor_property('intensity', item['intensity'])
            component.set_editor_property('use_temperature', True)
            component.set_editor_property('temperature', item['temperature'])
            if item['kind'] == 'directional_light':
                component.set_editor_property('light_source_angle', item['source_angle'])
            else:
                component.set_editor_property('attenuation_radius', item['radius'])

    require(levels.save_current_level(), 'Map save failed')
    require(levels.load_level(map_path), 'Saved map reload failed')
    labels = {a.get_actor_label() for a in actors.get_all_level_actors()
              if 'SorajiroGenerator_m1_1' in [str(t) for t in a.get_editor_property('tags')]}
    require(labels == {a['id'] for a in recipe['actors']}, 'Reloaded actor set mismatch')
    files = snapshot(output)
    required = {recipe['map_name'] + '.umap'} | {m['name'] + '.uasset' for m in recipe['materials']}
    require(required.issubset(files), 'Expected real binary packages were not saved')
    for item in recipe['materials']:
        require(assets.load_asset(recipe['output_root'] + '/' + item['name']), 'Saved material reload failed')
    report_dir.mkdir(parents=True, exist_ok=True)
    # Receipt is written only AFTER saving/reloading; an interrupted run never becomes a false pass.
    receipt_path.write_text(json.dumps({
        'checked_at_utc': datetime.now(timezone.utc).isoformat(),
        'generator_version': recipe['generator_version'], 'seed': recipe['seed'],
        'recipe_sha256': digest_recipe(recipe), 'engine_version': version,
        'files': files, 'state': 'generated_placeholder', 'saved_and_reloaded': True,
        'shader_verified': False, 'render_verified': False, 'cook_verified': False
    }, indent=2) + '\n')
    unreal.log('SORAJIRO generated and reloaded placeholders. Shader/render/cook still unverified.')


if __name__ == '__main__':
    run()
