"""Shared smooth sculpt deformation; Blender metres, Z-up, face -Y."""
import math


def coordinates(x, y, z, minimum, maximum, exp, sign):
    def smooth(t):
        t = minimum(1, maximum(0, t))
        return t*t*(3-2*t)
    belly = exp(-((z-.140)/.065)**4)*smooth((z-.065)/.035)*smooth((.215-z)/.035)
    spread = .008*smooth((.112-z)/.030)*smooth(abs(x)/.020)*sign(x)
    return x*(1+.12*belly)+spread, .003+(y-.003)*(1+.22*belly)-.003*belly, z


def point(x, y, z):
    return coordinates(x, y, z, min, max, math.exp, lambda x: (x > 0)-(x < 0))


def array(vertices, np):
    return np.column_stack(coordinates(*vertices.T, np.minimum, np.maximum, np.exp, np.sign))
