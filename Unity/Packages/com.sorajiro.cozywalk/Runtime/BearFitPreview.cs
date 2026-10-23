using UnityEngine;
using UnityEngine.InputSystem;

namespace Sorajiro
{
    // All modes retain the owner's preferred Fleece v005 material variant 2.
    public sealed class BearFitPreview : MonoBehaviour
    {
        public GameObject baselineModel,currentModel,baselineLights,currentLights;
        public Animator baselineAnimator,currentAnimator;
        public SorajiroMotor motor;
        public BearAnimationDriver driver;
        [Range(0,2)] public int variant=2;
        private void Start(){Apply(variant);}
        public void Apply(int selection)
        {
            variant=Mathf.Clamp(selection,0,2);
            bool shaped=variant==2,relit=variant!=0;
            baselineModel.SetActive(!shaped);currentModel.SetActive(shaped);
            baselineLights.SetActive(!relit);currentLights.SetActive(relit);
            motor.visualRoot=(shaped?currentModel:baselineModel).transform;
            driver.animator=shaped?currentAnimator:baselineAnimator;
        }
        private void Update()
        {
            var k=Keyboard.current;if(k==null)return;
            if(k.digit1Key.wasPressedThisFrame)Apply(0);
            if(k.digit2Key.wasPressedThisFrame)Apply(1);
            if(k.digit3Key.wasPressedThisFrame)Apply(2);
        }
    }
}
