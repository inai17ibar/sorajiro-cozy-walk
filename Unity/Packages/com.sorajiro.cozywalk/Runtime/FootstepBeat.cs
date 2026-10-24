using System;

namespace Sorajiro
{
    // Track actual Animator half-cycles. Never replay missed beats after a pause or a hitch.
    public sealed class FootstepBeat
    {
        private bool tracking;
        private double last;
        public bool Advance(double normalizedTime, bool movingOnGround)
        {
            if(!movingOnGround || double.IsNaN(normalizedTime) || double.IsInfinity(normalizedTime))
            {tracking=false;return false;}
            double beat=Math.Floor(normalizedTime*2);
            if(!tracking || beat<last){tracking=true;last=beat;return false;}
            bool crossed=beat>last;last=beat;return crossed;
        }
    }
}
