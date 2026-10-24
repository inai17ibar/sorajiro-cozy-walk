using System;

namespace Sorajiro
{
    public static class CameraComfortPolicy
    {
        public const float DefaultPitch=26, DefaultDistance=1.25f, TurnDegrees=45;
        public static float Turn(float yaw,int direction)
        {
            if(float.IsNaN(yaw)||float.IsInfinity(yaw))yaw=0;
            double value=(yaw+Math.Sign(direction)*TurnDegrees)%360;return (float)(value<0?value+360:value);
        }
        public static float Blend(float sharpness,float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return 0;
            return (float)(1-Math.Exp(-Math.Max(0,sharpness)*dt));
        }
    }
}
