"""Actual generator protection logic; these tests never emulate unreal/editor APIs."""
import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Tools/Pipeline'))
from recipe_contract import digest_recipe, generation_action, snapshot, validate


class RecipeContractTests(unittest.TestCase):
    def setUp(self):
        self.recipe = json.loads((ROOT / 'Tools/Unreal/recipes/bootstrap_v001.json').read_text())
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)

    def populate(self):
        # These are hash fixtures, not .uasset assets or a claim of editor generation.
        for name in [self.recipe['map_name'] + '.umap', *[m['name'] + '.uasset' for m in self.recipe['materials']]]:
            (self.folder / name).write_bytes(b'hash_fixture_only')
        return {'recipe_sha256': digest_recipe(self.recipe), 'engine_version': 'test-version',
                'files': snapshot(self.folder)}

    def test_empty_revision_can_create(self):
        self.assertEqual(generation_action(self.recipe, self.folder, None, 'test-version'), 'CREATE')

    def test_unchanged_rerun_is_read_only(self):
        receipt = self.populate()
        before = {p.name: p.stat().st_mtime_ns for p in self.folder.iterdir()}
        self.assertEqual(generation_action(self.recipe, self.folder, receipt, 'test-version'), 'REUSE')
        self.assertEqual(before, {p.name: p.stat().st_mtime_ns for p in self.folder.iterdir()})

    def test_owner_asset_without_receipt_is_preserved(self):
        owned = self.folder / 'owner.txt'
        owned.write_text('keep me')
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, None, 'test-version')
        self.assertEqual(owned.read_text(), 'keep me')

    def test_modified_generated_asset_refuses_overwrite(self):
        receipt = self.populate()
        (self.folder / 'M_PreviewBlue.uasset').write_bytes(b'owner_edit_fixture')
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'test-version')

    def test_extra_owner_file_refuses_overwrite(self):
        receipt = self.populate()
        (self.folder / 'owner.txt').write_text('keep')
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'test-version')

    def test_recipe_change_requires_new_revision(self):
        receipt = self.populate()
        self.recipe['actors'][0]['scale'][0] = 8
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'test-version')

    def test_engine_change_requires_new_revision(self):
        receipt = self.populate()
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'different-version')

    def test_deleted_package_not_reused(self):
        receipt = self.populate()
        (self.folder / 'L_Bootstrap.umap').unlink()
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'test-version')

    def test_incomplete_receipt_not_success(self):
        (self.folder / 'only.txt').write_text('partial')
        receipt = {'recipe_sha256': digest_recipe(self.recipe), 'engine_version': 'test-version',
                   'files': snapshot(self.folder)}
        with self.assertRaises(ValueError): generation_action(self.recipe, self.folder, receipt, 'test-version')

    def test_lfs_pointer_refused(self):
        (self.folder / 'asset.fixture').write_text('version https://git-lfs.github.com/spec/v1\noid sha256:fake\n')
        with self.assertRaises(ValueError): snapshot(self.folder)

    def test_symlinks_refused(self):
        (self.folder / 'link').symlink_to(ROOT / 'README.md')
        with self.assertRaises(ValueError): snapshot(self.folder)

    def test_output_path_traversal_or_owner_path_rejected(self):
        for destination in ('/Game/Sorajiro', '/Game/Generated/M1/../Owner', '/Game/Generated/M1/A/Nested'):
            with self.subTest(destination=destination):
                self.recipe['output_root'] = destination
                with self.assertRaises(ValueError): validate(self.recipe)

    def test_duplicate_actors_and_missing_material_rejected(self):
        original = copy.deepcopy(self.recipe)
        self.recipe['actors'].append(copy.deepcopy(self.recipe['actors'][0]))
        with self.assertRaises(ValueError): validate(self.recipe)
        original['actors'][0]['material'] = 'Missing'
        with self.assertRaises(ValueError): validate(original)

    def test_nonfinite_and_invalid_geometry_rejected(self):
        for value in (float('nan'), float('inf'), -1, True, 'wrong'):
            recipe = copy.deepcopy(self.recipe)
            recipe['actors'][0]['scale'][0] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError): validate(recipe)

    def test_config_map_and_material_links_match_recipe(self):
        package = self.recipe['output_root'] + '/' + self.recipe['map_name']
        self.assertIn('GameDefaultMap=' + package, (ROOT / 'Unreal/Config/DefaultEngine.ini').read_text())
        self.assertIn('FilePath="' + package + '"', (ROOT / 'Unreal/Config/DefaultGame.ini').read_text())
        self.assertIn(self.recipe['output_root'] + '/M_PreviewBlue.M_PreviewBlue',
                      (ROOT / 'Unreal/Config/DefaultGame.ini').read_text())


if __name__ == '__main__':
    unittest.main()
