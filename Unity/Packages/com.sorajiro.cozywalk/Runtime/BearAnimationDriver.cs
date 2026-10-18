using UnityEngine;

namespace Sorajiro
{
    // Physics owns world movement; Generic Animator only poses the editable rig.
    [DefaultExecutionOrder(100)]
    public sealed class BearAnimationDriver : MonoBehaviour
    {
        public SorajiroMotor motor;
        public Animator animator;
        private int resetVersion;
        private RuntimeAnimatorController checkedController;
        private bool hasCycleRate;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int CycleRate = Animator.StringToHash("CycleRate");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Vertical = Animator.StringToHash("VerticalSpeed");

        private void Update() { Sync(Time.deltaTime); }

        public void Sync(float dt)
        {
            if(!motor || !animator) return;
            animator.speed=motor.Paused || !motor.Focused ? 0 : 1;
            if(resetVersion!=motor.ResetVersion)
            {
                resetVersion=motor.ResetVersion;
                animator.Play("Locomotion",0,0);
            }
            animator.SetFloat(Speed,motor.PlanarSpeed,.06f,Mathf.Max(0,dt));
            if(checkedController!=animator.runtimeAnimatorController)
            {
                checkedController=animator.runtimeAnimatorController;hasCycleRate=false;
                foreach(var parameter in animator.parameters)if(parameter.nameHash==CycleRate){hasCycleRate=true;break;}
            }
            if(hasCycleRate)animator.SetFloat(CycleRate,PlushGait.CycleRate(animator.GetFloat(Speed),motor.walkSpeed));
            animator.SetBool(Grounded,motor.Grounded);
            animator.SetFloat(Vertical,motor.VerticalSpeed);
        }
    }
}
