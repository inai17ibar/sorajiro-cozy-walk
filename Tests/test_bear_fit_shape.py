import importlib.util
import math
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('bear_fit_shape', Path(__file__).resolve().parents[1]/'Tools/Blender/bear_fit_shape.py')
shape = importlib.util.module_from_spec(spec)
spec.loader.exec_module(shape)


class BearFitShapeTests(unittest.TestCase):
    def test_face_and_floor_preserved(self):
        for z in (.22, .25, .285, .365, .4):
            for x in (-.1, -.04, 0, .04, .1):
                self.assertEqual(shape.point(x, -.08, z), (x, -.08, z))
        self.assertEqual(shape.point(.042, -.01, .001)[2], .001)

    def test_stance_widens_without_scaling_the_sole(self):
        for x in (.022, .042, .065):
            self.assertAlmostEqual(shape.point(x, -.01, .025)[0]-x, .008)
        self.assertAlmostEqual(shape.point(.042, -.01, .025)[0]-shape.point(-.042, -.01, .025)[0], .100)

    def test_fuller_belly_without_global_scale(self):
        self.assertAlmostEqual(shape.point(.08, -.06, .14)[0], .08*1.12)
        front = shape.point(0, -.06, .14)[1]
        back = shape.point(0, .06, .14)[1]
        self.assertAlmostEqual(back-front, .12*1.22)
        self.assertLess(front, -.06)

    def test_symmetric_finite_and_no_lateral_fold(self):
        for zi in range(81):
            z = zi*.005
            previous = -math.inf
            for xi in range(-60,61):
                x = xi*.002
                result = shape.point(x, -.03, z)
                self.assertTrue(all(math.isfinite(v) for v in result))
                self.assertGreater(result[0], previous)
                previous = result[0]
                self.assertAlmostEqual(result[0], -shape.point(-x, -.03, z)[0])

    def test_transition_continuity(self):
        for z in (.065, .082, .100, .112, .180, .215):
            a = shape.point(.05, -.04, z-1e-7)
            b = shape.point(.05, -.04, z+1e-7)
            self.assertLess(math.dist(a,b), 1e-5)


if __name__ == '__main__': unittest.main()
