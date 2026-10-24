using System;
using Sorajiro.Editor;
static class CutePlushSfxTest
{
    static void Main()
    {
        int checks=0;Action<bool> check=v=>{checks++;if(!v)throw new Exception("Cute SFX assertion "+checks);};
        foreach(int kind in new[]{0,1,2})foreach(int variant in new[]{0,1,2})
        {
            var data=CutePlushSfx.Samples(kind,variant);double energy=0,maxJump=0;
            for(int i=0;i<data.Length;i++){check(!float.IsNaN(data[i])&&!float.IsInfinity(data[i])&&Math.Abs(data[i])<.8);energy+=data[i]*data[i];if(i>0)maxJump=Math.Max(maxJump,Math.Abs(data[i]-data[i-1]));}
            check(energy>1);check(maxJump<.08);check(data[0]==0&&data[data.Length-1]==0);
        }
        check(CutePlushSfx.Samples(0,0)[100]!=CutePlushSfx.Samples(0,1)[100]);
        check(CutePlushSfx.Samples(1).Length>CutePlushSfx.Samples(0).Length);
        Console.WriteLine("CutePlushSfx: "+checks+" assertions PASS; bounded finite non-silent output, smooth attack/tail and foot variations");
    }
}
