using System;
using Sorajiro;
public static class FootstepBeatTest
{
    public static void Main()
    {
        int checks=0;Action<bool> check=b=>{checks++;if(!b)throw new Exception("Footstep assertion "+checks);};var beat=new FootstepBeat();
        check(!beat.Advance(.1,true));check(!beat.Advance(.49,true));check(beat.Advance(.5,true));check(!beat.Advance(.51,true));check(beat.Advance(1,true));
        check(!beat.Advance(1.4,false));check(!beat.Advance(8.1,true));check(!beat.Advance(8.2,true));check(beat.Advance(8.5,true));
        check(!beat.Advance(.1,true));check(!beat.Advance(double.NaN,true));check(!beat.Advance(.5,true));check(beat.Advance(1,true));
        check(!beat.Advance(1,false));check(!beat.Advance(50,true));check(beat.Advance(90,true));check(!beat.Advance(90.01,true));
        Console.WriteLine("FootstepBeat: "+checks+" assertions PASS; no idle/air/resume catch-up, real half-cycle crossings");
    }
}
