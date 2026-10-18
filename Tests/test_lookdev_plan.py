"""Validate the handoff camera/asset plan; no claim of Unreal asset execution."""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class LookDevPlanTests(unittest.TestCase):
    def test_four_distinct_usable_cameras_and_explicit_unverified_assets(self):
        plan = json.loads((ROOT / 'Tools/Unreal/recipes/lookdev_plan_v001.json').read_text())
        self.assertEqual(plan['state'], 'PLANNED_NOT_GENERATED')
        self.assertEqual(plan['hero']['state'], 'NOT_CREATED')
        self.assertFalse(plan['hero']['blender_export']['axis_and_40cm_scale_verified'])
        expected = {'S01_Portrait', 'S02_Gameplay', 'S03_ThreeQuarter', 'S04_Contact'}
        self.assertEqual({c['id'] for c in plan['cameras']}, expected)
        self.assertEqual(len(plan['cameras']), 4)
        for camera in plan['cameras']:
            with self.subTest(shot=camera['id']):
                for vector in (camera['location_cm'], camera['look_at_cm']):
                    self.assertEqual(len(vector), 3)
                    self.assertTrue(all(math.isfinite(v) for v in vector))
                self.assertGreater(sum((a - b)**2 for a, b in zip(camera['location_cm'], camera['look_at_cm'])), 1)
                self.assertTrue(50 <= camera['horizontal_fov_deg'] <= 65)
        self.assertEqual({e['role'] for e in plan['elements']},
                         {'bear', 'window_and_warm_key', 'wood_floor_beveled_planks', 'rug_thickness_contact'})
        self.assertTrue(all(e['state'] == 'NOT_CREATED' for e in plan['elements']))
        self.assertTrue(plan['capture']['real_UE_capture_only'])
        self.assertFalse(plan['capture']['DOF'])


if __name__ == '__main__':
    unittest.main()
