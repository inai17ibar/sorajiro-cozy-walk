using UnityEngine;
using UnityEngine.InputSystem;

namespace Sorajiro
{
    [RequireComponent(typeof(Camera))]
    public sealed class OrbitCamera : MonoBehaviour
    {
        public SorajiroMotor target;
        public float sensitivity = 0.15f;
        public float distance = 1.2f;
        public float wheelUnitsPerNotch = 120;
        public float yaw;
        public float pitch = 15;
        public bool comfortControls;
        public float ActualDistance { get; private set; }
        private Vector3 smoothPivot;
        private float smoothYaw,smoothPitch,homeYaw;
        private int resetVersion;
        private bool initialized;

        public void ConfigureComfort()
        {
            comfortControls=true;homeYaw=yaw;pitch=CameraComfortPolicy.DefaultPitch;distance=CameraComfortPolicy.DefaultDistance;
            initialized=false;Follow(0);
        }
        public void TurnStep(int direction)
        {
            if(!comfortControls || !target || target.Paused || !target.Focused)return;
            yaw=CameraComfortPolicy.Turn(yaw,direction);
        }
        public void Recenter()
        {
            if(!comfortControls || !target || target.Paused || !target.Focused)return;
            yaw=target.visualRoot?target.visualRoot.eulerAngles.y:homeYaw;
            pitch=CameraComfortPolicy.DefaultPitch;distance=CameraComfortPolicy.DefaultDistance;
        }

        private void LateUpdate()
        {
            if (!target) return;
            if(comfortControls && (target.Paused || !target.Focused))return;
            Mouse mouse = Mouse.current;
            if (!target.Paused && target.Focused && mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360);
                    pitch = MotionPolicy.Pitch(pitch, -delta.y * sensitivity);
                }
                distance = MotionPolicy.Zoom(distance, mouse.scroll.ReadValue().y / Mathf.Max(1, wheelUnitsPerNotch));
                if(comfortControls)
                {
                    Vector2 scroll=mouse.scroll.ReadValue();
                    if(Mathf.Abs(scroll.x)>Mathf.Abs(scroll.y))yaw=Mathf.Repeat(yaw+12*scroll.x/Mathf.Max(1,wheelUnitsPerNotch),360);
                }
            }
            if(comfortControls && Keyboard.current!=null)
            {
                if(Keyboard.current.qKey.wasPressedThisFrame)TurnStep(-1);
                if(Keyboard.current.eKey.wasPressedThisFrame)TurnStep(1);
                if(Keyboard.current.cKey.wasPressedThisFrame)Recenter();
            }
            Follow(Time.deltaTime);
        }

        // The same follow/collision path is used in gameplay and actual scene verification.
        public void Follow(float dt)
        {
            if(!target || (comfortControls && initialized && (target.Paused || !target.Focused)))return;
            Vector3 pivot = target.transform.position + Vector3.up * 0.25f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            if(comfortControls)
            {
                if(!initialized || resetVersion!=target.ResetVersion)
                {
                    if(initialized){yaw=homeYaw;pitch=CameraComfortPolicy.DefaultPitch;distance=CameraComfortPolicy.DefaultDistance;}
                    smoothPivot=pivot;smoothYaw=yaw;smoothPitch=pitch;initialized=true;resetVersion=target.ResetVersion;
                }
                float turn=CameraComfortPolicy.Blend(13,dt),follow=CameraComfortPolicy.Blend(16,dt);
                smoothYaw=Mathf.LerpAngle(smoothYaw,yaw,turn);smoothPitch=Mathf.LerpAngle(smoothPitch,pitch,turn);
                smoothPivot=new Vector3(Mathf.Lerp(smoothPivot.x,pivot.x,follow),Mathf.Lerp(smoothPivot.y,pivot.y,CameraComfortPolicy.Blend(9,dt)),Mathf.Lerp(smoothPivot.z,pivot.z,follow));
                rotation=Quaternion.Euler(smoothPitch,smoothYaw,0);
            }
            Vector3 backward = rotation * Vector3.back;
            Vector3 desired=(comfortControls?smoothPivot:pivot)+backward*distance;
            Vector3 ray=desired-pivot;
            float safeDistance = distance;
            // Player uses Ignore Raycast; environment colliders stay in Default.
            if (Physics.SphereCast(pivot, 0.04f, ray.normalized, out RaycastHit hit, ray.magnitude,
                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {safeDistance=Mathf.Max(0.01f,hit.distance-.005f);desired=pivot+ray.normalized*safeDistance;}
            ActualDistance=Vector3.Distance(pivot,desired);
            transform.SetPositionAndRotation(desired,rotation);
        }
        private void OnGUI()
        {
            if(!comfortControls || !target)return;
            bool enabled=GUI.enabled;GUI.enabled=!target.Paused&&target.Focused;
            float y=Screen.height-48;
            if(GUI.Button(new Rect(16,y,100,32),"左へ  Q"))TurnStep(-1);
            if(GUI.Button(new Rect(122,y,122,32),"後ろへ戻す  C"))Recenter();
            if(GUI.Button(new Rect(250,y,100,32),"右へ  E"))TurnStep(1);
            GUI.enabled=enabled;
        }
    }
}
