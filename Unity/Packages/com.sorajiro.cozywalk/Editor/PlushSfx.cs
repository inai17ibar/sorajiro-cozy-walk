using System;
using System.IO;

namespace Sorajiro.Editor
{
    // Original synthetic foley. No external recordings, services or redistribution licenses.
    public static class PlushSfx
    {
        public const int Rate=48000;
        public static float[] Samples(int kind,int variant=0)
        {
            int count=(int)(Rate*(kind==1?.8:kind==2?.22:.14));var data=new float[count];
            uint seed=(uint)(27183+variant*671);double filtered=0;
            for(int i=0;i<count;i++)
            {
                double t=(double)i/Rate,v=0;
                if(kind==1)
                {
                    double[] frequencies={1046.5023,1318.5102,1567.9817,2093.0045};
                    for(int n=0;n<4;n++)
                    {
                        double u=t-n*.055;
                        if(u>=0)v+=.12*(1-Math.Exp(-u*900))*Math.Exp(-u*8)*
                            (Math.Sin(2*Math.PI*frequencies[n]*u)+.22*Math.Sin(2*Math.PI*frequencies[n]*2.003*u));
                    }
                }
                else
                {
                    seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;
                    double noise=(seed/(double)uint.MaxValue)*2-1;filtered+=.12*(noise-filtered);
                    double attack=1-Math.Exp(-t*1400),decay=Math.Exp(-t*(kind==2?27:43));
                    v=attack*decay*(.16*Math.Sin(2*Math.PI*(kind==2?75:108+variant*7)*t)+.20*filtered);
                }
                double tail=Math.Min(1,(count-1-i)/240.0);data[i]=(float)(v*tail);
            }
            return data;
        }
        public static void WriteWav(string path,float[] samples,int channels=1,int rate=Rate)
        {
            using(var w=new BinaryWriter(File.Create(path)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+samples.Length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);w.Write((short)1);w.Write((short)channels);w.Write(rate);w.Write(rate*channels*2);w.Write((short)(channels*2));w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(samples.Length*2);
                foreach(float v in samples)w.Write((short)(Math.Max(-1,Math.Min(1,v))*32767));
            }
        }
    }
}
