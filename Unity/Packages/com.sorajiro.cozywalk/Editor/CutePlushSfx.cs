using System;

namespace Sorajiro.Editor
{
    // Original rounded cartoon foley: a soft puff, rising hop and a small springy landing.
    public static class CutePlushSfx
    {
        public static float[] Samples(int kind,int variation=0)
        {
            const int rate=48000;double duration=kind==1?.22:kind==2?.24:.115;
            var samples=new float[(int)(rate*duration)];uint seed=(uint)(3907+variation*103);double phase=0,noise=0;
            for(int i=0;i<samples.Length;i++)
            {
                double t=i/(double)rate,u=t/duration;
                double frequency=kind==1?310+430*Math.Min(1,t/.17):kind==2?165+125*Math.Exp(-t*35):200+variation*12+130*Math.Exp(-t*48);
                phase+=2*Math.PI*frequency/rate;
                seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;noise+=.18*((seed/(double)uint.MaxValue*2-1)-noise);
                double attack=1-Math.Exp(-t*(kind==1?240:600));double decay=Math.Exp(-t*(kind==1?13:kind==2?24:48));
                double sound=.24*attack*decay*(Math.Sin(phase)+.12*Math.Sin(phase*2)+.16*noise);
                if(kind==2 && t>.055){double s=t-.055;sound+=.05*(1-Math.Exp(-s*400))*Math.Exp(-s*35)*Math.Sin(2*Math.PI*260*s);}
                samples[i]=(float)(sound*Math.Min(1,(1-u)*duration*rate/480));
            }
            samples[0]=samples[samples.Length-1]=0;return samples;
        }
    }
}
