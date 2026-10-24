using System;
using Sorajiro;
static class CameraComfortTest
{
    static void Main()
    {
        int checks=0;Action<bool> check=v=>{checks++;if(!v)throw new Exception("Camera policy assertion "+checks);};
        check(CameraComfortPolicy.DefaultPitch>15&&CameraComfortPolicy.DefaultPitch<40);
        check(CameraComfortPolicy.Turn(350,1)==35);check(CameraComfortPolicy.Turn(10,-1)==325);check(CameraComfortPolicy.Turn(10,0)==10);
        check(CameraComfortPolicy.Turn(float.NaN,1)==45);check(CameraComfortPolicy.Turn(float.PositiveInfinity,-1)==315);
        check(CameraComfortPolicy.Blend(13,0)==0);check(CameraComfortPolicy.Blend(13,float.NaN)==0);check(CameraComfortPolicy.Blend(13,-1)==0);
        Func<int,float> simulate=fps=>{float value=0;for(int i=0;i<fps;i++)value+=(1-value)*CameraComfortPolicy.Blend(13,1f/fps);return value;};
        check(Math.Abs(simulate(30)-simulate(60))<.000001);check(Math.Abs(simulate(60)-simulate(144))<.000001);
        for(int i=0;i<8;i++)check(CameraComfortPolicy.Turn(45*i,1)==(45*(i+1))%360);
        Console.WriteLine("CameraComfort: "+checks+" assertions PASS; frame-independent smoothing, 45 degree steps, wrap and invalid-time guards");
    }
}
