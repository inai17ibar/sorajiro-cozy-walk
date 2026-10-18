using UnityEngine;
using UnityEngine.InputSystem;

namespace Sorajiro
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SorajiroMotor : MonoBehaviour
    {
        public Transform view;
        public Transform visualRoot;
        public float walkSpeed = 0.7f;
        public float runSpeed = 1.2f;
        public float jumpHeight = 0.14f;
        public string diagnosticLabel = "M1 仮モデル：毛・全身リグ・歩行アニメーション未実装";
        public bool Paused { get; private set; }
        public bool Focused { get; private set; } = true;
        public float PlanarSpeed { get; private set; }
        public float VerticalSpeed => verticalSpeed;
        public bool Grounded => controller && controller.isGrounded;
        public int ResetVersion { get; private set; }
        private CharacterController controller;
        private Vector3 spawn;
        private float verticalSpeed;
        private GUIStyle label;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
        }

        private void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys == null || !Focused) return;
            if (keys.escapeKey.wasPressedThisFrame) Paused = !Paused;
            if (Paused) return;
            if (keys.rKey.wasPressedThisFrame) { ReturnToStart(); return; }
            float x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) -
                      (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            float y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) -
                      (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
            Step(x, y, keys.spaceKey.wasPressedThisFrame,
                 keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed, Time.deltaTime);
        }

        public void Step(float x, float y, bool jump, bool run, float dt)
        {
            if (Paused || !Focused || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            MotionPolicy.NormalizeMovement(ref x, ref y);
            Vector3 forward = view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * y + right * x;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -0.5f;
            if (controller.isGrounded && jump) verticalSpeed = Mathf.Sqrt(2 * 9.81f * jumpHeight);
            verticalSpeed -= 9.81f * dt;
            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move((direction * (run ? runSpeed : walkSpeed) +
                                                     Vector3.up * verticalSpeed) * dt);
            PlanarSpeed = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude / dt;
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if (visualRoot && direction.sqrMagnitude > 0.001f)
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                    Quaternion.LookRotation(direction), 540 * dt);
            if (transform.position.y < -2) ReturnToStart();
        }

        public void ReturnToStart()
        {
            controller.enabled = false;
            transform.position = spawn;
            controller.enabled = true;
            verticalSpeed = 0;
            PlanarSpeed = 0;
            ResetVersion++;
        }

        private void OnApplicationFocus(bool focused) { Focused = focused; }

        private void OnGUI()
        {
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true,
                    alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0) };
                label.normal.textColor = Color.white;
            }
            string text = diagnosticLabel + "\n" +
                "WASD／矢印：移動　Shift：走る　Space：ジャンプ\n" +
                "右ドラッグ：カメラ　ホイール：ズーム　R：復帰　Esc：一時停止" +
                (Paused ? "\n一時停止中" : "");
            float width = Mathf.Max(100, Mathf.Min(Screen.width - 48, 635));
            float height = label.CalcHeight(new GUIContent(text), width);
            GUI.Box(new Rect(12, 12, width + 24, height + 16), GUIContent.none);
            GUI.Label(new Rect(24, 19, width, height), text, label);
        }
    }
}
