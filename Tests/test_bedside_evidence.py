"""Baseline evidence must distinguish exact engine migration from owner edits."""
import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('bedside_evidence', Path(__file__).resolve().parents[1]/'scripts/collect_bedside_evidence.py')
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)


class BaselineMigrationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.folder = Path(self.tmp.name)
        self.original = 'settings:\n    m_Version: 0\n    rayOriginBias: 0\n    m_Bits: 0\n  m_LightingScenarios: []\n  sceneReference: keep\n'
        self.path = self.folder/'APV_BakingSet.asset'
        self.path.write_text(self.original)
        (self.folder/'L_LookDev.unity').write_text('fixture scene; not a real Unity asset')
        files = ['APV_BakingSet.asset', 'L_LookDev.unity']
        receipt = {'files': files, 'hashes': [hashlib.sha256((self.folder/f).read_bytes()).hexdigest() for f in files]}
        (self.folder/'generation-receipt.json').write_text(json.dumps(receipt))

    def migrate(self):
        self.path.write_text(self.original.replace('m_Version: 0','m_Version: 1')
            .replace('rayOriginBias: 0','rayOriginBias: -0.001')
            .replace('m_Bits: 0','m_Bits: 4294967291')
            .replace('  m_LightingScenarios: []','  m_LightingScenarios:\n  - Default'))

    def test_unchanged(self):
        self.assertEqual(evidence.baseline_integrity(self.folder)['state'], 'byte_identical')

    def test_exact_installed_migration_is_reported_without_rewriting(self):
        self.migrate()
        before = {p.name: p.read_bytes() for p in self.folder.iterdir()}
        result = evidence.baseline_integrity(self.folder)
        self.assertTrue(result['inverse_migration_exactly_matches_original_hash'])
        self.assertIn('not_byte_identical', result['state'])
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.folder.iterdir()})

    def test_unexpected_apv_edit_is_not_excused_as_migration(self):
        self.migrate()
        self.path.write_text(self.path.read_text().replace('sceneReference: keep','sceneReference: edited'))
        with self.assertRaises(AssertionError):
            evidence.baseline_integrity(self.folder)

    def test_owner_scene_edit_is_rejected(self):
        self.migrate()
        (self.folder/'L_LookDev.unity').write_text('owner edit')
        with self.assertRaises(AssertionError):
            evidence.baseline_integrity(self.folder)

    def test_added_owner_file_is_rejected(self):
        (self.folder/'notes.txt').write_text('preserve me')
        with self.assertRaises(AssertionError):
            evidence.baseline_integrity(self.folder)
