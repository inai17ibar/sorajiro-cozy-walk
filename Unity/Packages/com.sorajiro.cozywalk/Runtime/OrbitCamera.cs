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

        private void LateUpdate()
        {
            if (!target) return;
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
            }
            Vector3 pivot = target.transform.position + Vector3.up * 0.25f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 backward = rotation * Vector3.back;
            float safeDistance = distance;
            // Player uses Ignore Raycast; environment colliders stay in Default.
            if (Physics.SphereCast(pivot, 0.04f, backward, out RaycastHit hit, distance,
                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                safeDistance = Mathf.Max(0.01f, hit.distance - 0.005f);
            transform.SetPositionAndRotation(pivot + backward * safeDistance, rotation);
        }
    }
}
