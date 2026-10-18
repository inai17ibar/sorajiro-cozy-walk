using System;
using Sorajiro;

static class MotionPolicyTest
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
    static void Main()
    {
        float x = 1, y = 1;
        MotionPolicy.NormalizeMovement(ref x, ref y);
        Check(Near(x*x+y*y, 1), "diagonal speed exceeds cardinal speed");
        x = float.NaN; y = float.PositiveInfinity;
        MotionPolicy.NormalizeMovement(ref x, ref y);
        Check(x == 0 && y == 0, "invalid movement must be neutral");
        Check(MotionPolicy.Pitch(0, 10000) == 65, "upper pitch bound");
        Check(MotionPolicy.Pitch(0, -10000) == -15, "lower pitch bound");
        Check(MotionPolicy.Pitch(float.NaN, float.NaN) == 15, "pitch recovery");
        Check(MotionPolicy.Zoom(1, 10000) == MotionPolicy.NearDistance, "zoom near bound");
        Check(MotionPolicy.Zoom(1, -10000) == MotionPolicy.FarDistance, "zoom far bound");
        Check(MotionPolicy.Zoom(float.NaN, 0) == 1.2f, "zoom recovery");
        float once = MotionPolicy.Zoom(1.2f, 1);
        float split = 1.2f;
        for (int i = 0; i < 10; ++i) split = MotionPolicy.Zoom(split, .1f);
        Check(Near(once, split), "fractional trackpad deltas must compose");
        Console.WriteLine("PASS: production MotionPolicy diagonal, invalid input, bounds, fractional zoom (9 assertions)");
    }
}
