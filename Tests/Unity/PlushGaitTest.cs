using System;
using Sorajiro;

static class PlushGaitTest
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Main()
    {
        foreach(bool run in new[]{false,true})
        {
            float speed=run?.52f:.28f, period=run?.40f:.52f;
            var a=PlushGait.Sample(.12f,speed,period,run);
            var b=PlushGait.Sample(.30f,speed,period,run);
            Check(a.Planted && b.Planted,"support phase required");
            Check(Math.Abs(b.Forward-a.Forward+speed*period*.18f)<1e-6f,"support foot must remain stationary in world travel");
            Check(a.Height==0 && b.Height==0 && a.Pitch==0 && b.Pitch==0,"flat support must not bob or pitch");
            var c=PlushGait.Sample(.8f,speed,period,run);
            Check(!c.Planted && c.Height>.01f,"swing must clear the floor");
            var d=PlushGait.Sample(1.12f,speed,period,run);
            Check(Math.Abs(a.Forward-d.Forward)<1e-6f,"cycle must repeat");
            for(int i=0;i<1000;i++)Check(PlushGait.Sample(i/1000f,speed,period,run).Height>=-1e-6,"no foot trajectory below floor");
        }
        Check(Math.Abs(PlushGait.CycleRate(.14f,.28f)-.5f)<1e-6,"slow motion cadence must follow speed");
        bool rejected=false;try{PlushGait.Sample(float.NaN,.28f,.52f,false);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"invalid phase must be refused");
        Console.WriteLine("PASS: production PlushGait planted world velocity, flat support, swing clearance, loops, floor clearance and cycle rate (2012 assertions)");
    }
}
