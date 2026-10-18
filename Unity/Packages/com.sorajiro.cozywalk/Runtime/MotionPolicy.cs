using System;

namespace Sorajiro
{
    // Actual runtime policy, also tested without loading a Unity scene.
    public static class MotionPolicy
    {
        public const float NearDistance = 0.55f;
        public const float FarDistance = 2.4f;

        public static float Pitch(float pitch, float delta)
        {
            if (!Finite(pitch)) pitch = 15;
            if (!Finite(delta)) delta = 0;
            return Clamp(pitch + delta, -15, 65);
        }

        public static float Zoom(float distance, float notches)
        {
            if (!Finite(distance)) distance = 1.2f;
            if (!Finite(notches)) notches = 0;
            return Clamp(Clamp(distance, NearDistance, FarDistance) *
                         (float)Math.Exp(-0.12 * Clamp(notches, -100, 100)), NearDistance, FarDistance);
        }

        public static void NormalizeMovement(ref float x, ref float y)
        {
            x = Finite(x) ? Clamp(x, -1, 1) : 0;
            y = Finite(y) ? Clamp(y, -1, 1) : 0;
            float length = (float)Math.Sqrt(x * x + y * y);
            if (length > 1) { x /= length; y /= length; }
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        private static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
    }
}
