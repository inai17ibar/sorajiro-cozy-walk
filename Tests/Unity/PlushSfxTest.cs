using System;
using Sorajiro.Editor;
public static class PlushSfxTest
{
    public static void Main()
    {
        int checks=0;Action<bool> check=b=>{checks++;if(!b)throw new Exception("Sfx assertion "+checks);};
        foreach(int kind in new[]{0,1,2})foreach(int variant in new[]{0,1,2})
        {
            var samples=PlushSfx.Samples(kind,variant);double energy=0,peak=0;
            foreach(float value in samples){check(!float.IsNaN(value)&&!float.IsInfinity(value));energy+=value*value;peak=Math.Max(peak,Math.Abs(value));}
            check(samples.Length>4800&&samples.Length<=48000);check(energy>1);check(peak<.8);check(samples[0]==0&&samples[samples.Length-1]==0);
        }
        var a=PlushSfx.Samples(0,0);var b=PlushSfx.Samples(0,1);check(a[100]!=b[100]);
        Console.WriteLine("PlushSfx: "+checks+" assertions PASS; finite, non-silent, bounded peaks, click-free ends, varied steps");
    }
}
