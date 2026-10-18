using System;

namespace Sorajiro
{
    // Used directly by editable clip generation and runtime cycle-rate synchronization.
    public static class PlushGait
    {
        public readonly struct Foot
        {
            public readonly float Forward, Height, Pitch;
            public readonly bool Planted;
            public Foot(float forward,float height,float pitch,bool planted)
            {Forward=forward; Height=height; Pitch=pitch; Planted=planted;}
        }

        public static Foot Sample(float phase,float speed,float period,bool run)
        {
            if(float.IsNaN(phase) || float.IsInfinity(phase) || speed<0 || period<=0 ||
               float.IsNaN(speed) || float.IsInfinity(speed) || float.IsNaN(period) || float.IsInfinity(period))
                throw new ArgumentOutOfRangeException("Finite phase, nonnegative speed and positive period required.");
            phase-=(float)Math.Floor(phase);
            float stance=run?.46f:.62f;
            float stride=speed*period*stance;
            float z,y,pitch;
            if(phase<stance)
            {
                // Constant backwards velocity cancels the controller's forward travel.
                z=stride*.5f-speed*period*phase; y=0;
                float u=phase/stance;
                pitch=u<.12f?-4*(1-u/.12f):u>.84f?8*(u-.84f)/.16f:0;
            }
            else
            {
                float u=(phase-stance)/(1-stance), smooth=u*u*(3-2*u);
                z=-stride*.5f+stride*smooth;
                y=(run?.023f:.016f)*(float)Math.Sin(Math.PI*u);
                pitch=8-12*smooth;
            }
            float radians=pitch*(float)Math.PI/180;
            y+=.025f*((float)Math.Cos(radians)-1)+.031f*Math.Abs((float)Math.Sin(radians));
            return new Foot(z,y,pitch,phase<stance);
        }

        public static float CycleRate(float speed,float walkSpeed)
        {return speed<.005f?1:Math.Max(.02f,Math.Min(1,speed/walkSpeed));}
    }
}
