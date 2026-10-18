"""Engine-independent validation/protection shared by the real editor generator."""
from __future__ import annotations

import hashlib
import json
import math
import re
from pathlib import Path

KINDS = {'cube', 'player_start', 'directional_light', 'point_light'}
NAME = re.compile(r'^[A-Za-z][A-Za-z0-9_]*$')
ROOT = re.compile(r'^/Game/Generated/M1/[A-Za-z][A-Za-z0-9_]*$')


def number(value, low, high):
    if isinstance(value, bool) or not isinstance(value, (float, int)) or not math.isfinite(value):
        raise ValueError('Expected finite number')
    if not low <= value <= high:
        raise ValueError(f'Number outside [{low}, {high}]')


def vector(value, low, high):
    if not isinstance(value, list) or len(value) != 3:
        raise ValueError('Expected three-component vector')
    for component in value:
        number(component, low, high)


def validate(recipe):
    if recipe['schema_version'] != 1 or recipe['generator_version'] != 'm1.1':
        raise ValueError('Unsupported schema or generator version')
    if recipe['units'] != 'cm' or recipe['axes'] != 'Z-up, +X forward':
        raise ValueError('Unsupported units/axes')
    if not ROOT.fullmatch(recipe['output_root']) or not NAME.fullmatch(recipe['map_name']):
        raise ValueError('Output must be a named revision within /Game/Generated/M1')
    if recipe['asset_state'] != 'placeholder' or recipe['seed'] != 0:
        raise ValueError('M1 recipe must explicitly identify deterministic placeholders')
    names = set()
    for material in recipe['materials']:
        name = material['name']
        if not NAME.fullmatch(name) or name in names:
            raise ValueError('Invalid/duplicate material name')
        names.add(name)
        vector(material['linear_color'], 0, 1)
        number(material['roughness'], 0, 1)
    if recipe['map_name'] in names:
        raise ValueError('Map/material package name collision')
    ids = set()
    for actor in recipe['actors']:
        if not NAME.fullmatch(actor['id']) or actor['id'] in ids or actor['kind'] not in KINDS:
            raise ValueError('Invalid/duplicate actor ID or unsupported kind')
        ids.add(actor['id'])
        vector(actor['location'], -10000, 10000)
        vector(actor.get('rotation', [0, 0, 0]), -360, 360)
        if actor['kind'] == 'cube':
            vector(actor['scale'], 0.001, 100)
            if actor['material'] not in names:
                raise ValueError('Missing material reference')
        if actor['kind'].endswith('_light'):
            number(actor['intensity'], 0, 100000)
            number(actor['temperature'], 1000, 15000)
        if actor['kind'] == 'directional_light':
            number(actor['source_angle'], 0, 20)
        if actor['kind'] == 'point_light':
            number(actor['radius'], 1, 10000)
    if sum(a['kind'] == 'player_start' for a in recipe['actors']) != 1:
        raise ValueError('Exactly one player start is required')
    if not any(a['kind'] == 'cube' for a in recipe['actors']):
        raise ValueError('Collision geometry required')
    return recipe


def digest_recipe(recipe):
    return hashlib.sha256(json.dumps(recipe, sort_keys=True, separators=(',', ':'),
                                     allow_nan=False).encode()).hexdigest()


def content_directory(project_dir, recipe):
    validate(recipe)
    return Path(project_dir) / 'Content' / recipe['output_root'].removeprefix('/Game/')


def snapshot(directory):
    """Include every file, including sidecars and new owner content, in protection."""
    directory = Path(directory)
    if directory.is_symlink():
        raise ValueError('Generated directory cannot be a symlink')
    result = {}
    if directory.exists():
        for path in sorted(directory.rglob('*')):
            if path.is_symlink():
                raise ValueError('Symlink in generated directory')
            if path.is_file():
                data = path.read_bytes()
                if data.startswith(b'version https://git-lfs.github.com/spec/v1'):
                    raise ValueError('LFS pointer is not an asset: ' + path.name)
                result[str(path.relative_to(directory))] = hashlib.sha256(data).hexdigest()
    return result


def generation_action(recipe, directory, receipt, engine_version):
    """Create only into an empty revision; unchanged reruns perform no writes."""
    validate(recipe)
    current = snapshot(directory)
    if receipt is None:
        if current:
            raise ValueError('Existing content without receipt; preserve it and use a new revision')
        return 'CREATE'
    if (receipt.get('recipe_sha256') != digest_recipe(recipe)
            or receipt.get('engine_version') != engine_version
            or receipt.get('files') != current or not current):
        raise ValueError('Recipe/engine/content changed; no overwrite allowed, use a new revision')
    required = {recipe['map_name'] + '.umap'} | {m['name'] + '.uasset' for m in recipe['materials']}
    if not required.issubset(current):
        raise ValueError('Receipt does not contain required real packages')
    return 'REUSE'
