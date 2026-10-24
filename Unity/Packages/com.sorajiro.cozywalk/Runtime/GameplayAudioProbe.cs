using System;
using UnityEngine;

namespace Sorajiro
{
    // Bounded verification tap for generated game output only; never accesses a microphone.
    public sealed class GameplayAudioProbe : MonoBehaviour
    {
        private readonly object gate=new object();
        private float[] samples;
        private int count,channels;
        public void Begin(int maximumSamples){lock(gate){samples=new float[maximumSamples];count=channels=0;}}
        private void OnAudioFilterRead(float[] data,int channelCount)
        {
            lock(gate)
            {
                if(samples==null)return;
                channels=channelCount;int n=Math.Min(data.Length,samples.Length-count);
                Array.Copy(data,0,samples,count,n);count+=n;
            }
        }
        public float[] Snapshot(out int channelCount)
        {lock(gate){channelCount=channels;var result=new float[count];if(count>0)Array.Copy(samples,result,count);return result;}}
    }
}
