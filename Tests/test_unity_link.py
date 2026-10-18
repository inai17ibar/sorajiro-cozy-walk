import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('link', Path(__file__).parents[1] / 'scripts/link_unity_package.py')
link = importlib.util.module_from_spec(spec)
spec.loader.exec_module(link)

class LinkTests(unittest.TestCase):
    def setUp(self):
        self.manifest = {'dependencies': {'com.unity.render-pipelines.high-definition': '17.6.0',
            'com.unity.inputsystem': '1.20.0', 'owner.package': 'file:../owner'}, 'scopedRegistries': [{'name': 'owner'}]}

    def test_preserves_all_existing_fields_and_is_idempotent(self):
        before = copy.deepcopy(self.manifest)
        changed = link.planned_manifest(self.manifest, Path('/project/local package'))
        self.assertEqual(self.manifest, before)
        self.assertEqual(changed['scopedRegistries'], before['scopedRegistries'])
        for key, value in before['dependencies'].items(): self.assertEqual(changed['dependencies'][key], value)
        self.assertEqual(link.planned_manifest(changed, Path('/project/local package')), changed)

    def test_preserves_other_sorajiro_dependency(self):
        self.manifest['dependencies'][link.NAME] = 'file:../owner-sorajiro'
        with self.assertRaises(ValueError): link.planned_manifest(self.manifest, Path('/ours'))

    def test_rejects_unreviewed_hdrp_version(self):
        self.manifest['dependencies']['com.unity.render-pipelines.high-definition'] = '18.0.0'
        with self.assertRaises(ValueError): link.planned_manifest(self.manifest, Path('/ours'))

if __name__ == '__main__': unittest.main()
