using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorajiro.Editor
{
    public static class MotorSmokeTests
    {
        [MenuItem("Sorajiro/7. Test motor physics (Play, resets player)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var motor = UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();
            if (!motor || motor.Paused || !motor.Focused)
                throw new InvalidOperationException("A focused, unpaused player is required.");
            bool m2=motor.gameObject.scene.path==M2LookDevBuilder.ScenePath;
            bool twilight=motor.gameObject.scene.path==TwilightLookDevBuilder.ScenePath || motor.gameObject.scene.path==FleeceStudyBuilder.ScenePath || motor.gameObject.scene.path==BearFitBuilder.ScenePath;
            bool bedside=motor.gameObject.scene.path==BedsideBearBuilder.ScenePath || motor.gameObject.scene.path==BedsideCornerBuilder.ScenePath || (motor.gameObject.scene.path==BeddingRefinementBuilder.ScenePath || motor.gameObject.scene.path==BeddingContactBuilder.ScenePath || motor.gameObject.scene.path==BeddingDrapeBuilder.ScenePath || motor.gameObject.scene.path==BeddingSurfaceBuilder.ScenePath || motor.gameObject.scene.path==BeddingRoundBuilder.ScenePath || motor.gameObject.scene.path==BeddingStableBuilder.ScenePath);
            if (!m2 && !twilight && !bedside && motor.gameObject.scene.path != GeneratedContentGuard.ScenePath)
                throw new InvalidOperationException("Run only in the generated M1, M2 or Twilight test scene.");
            motor.ReturnToStart(); Vector3 expectedSpawn=motor.transform.position;
            var controller = motor.GetComponent<CharacterController>();
            Quaternion visualRotation = motor.visualRoot ? motor.visualRoot.rotation : Quaternion.identity;
            try
            {
                Vector3 Settle()
                {
                    motor.ReturnToStart();
                    Physics.SyncTransforms();
                    for (int i = 0; i < 60; ++i) motor.Step(0, 0, false, false, 1f/60);
                    Check(controller.isGrounded, "grounding");
                    return motor.transform.position;
                }
                float Travel(float x, float y, bool run, int steps, float dt)
                {
                    Vector3 origin = Settle();
                    for (int i = 0; i < steps; ++i) motor.Step(x, y, false, run, dt);
                    return Vector3.ProjectOnPlane(motor.transform.position - origin, Vector3.up).magnitude;
                }
                float cardinal = Travel(0, 1, false, 20, 1f/60);
                float diagonal = Travel(1, 1, false, 20, 1f/60);
                float lowerRate = Travel(0, 1, false, 10, 1f/30);
                float running = Travel(0, 1, true, 20, 1f/60);
                Check(Mathf.Abs(cardinal - motor.walkSpeed/3) < .01f, "walk speed");
                Check(Mathf.Abs(diagonal - cardinal) < .01f, "diagonal speed");
                Check(Mathf.Abs(lowerRate - cardinal) < .01f, "30/60 step-rate movement");
                Check(Mathf.Abs(running - motor.runSpeed/3) < .01f, "run speed");

                float JumpPeak(bool secondAirJump)
                {
                    Vector3 origin = Settle();
                    float peak = origin.y;
                    for (int i = 0; i < 90; ++i)
                    {
                        motor.Step(0, 0, i == 0 || (secondAirJump && i == 1), false, 1f/60);
                        peak = Mathf.Max(peak, motor.transform.position.y);
                    }
                    Check(controller.isGrounded, "jump landing");
                    return peak - origin.y;
                }
                float peak = JumpPeak(false);
                float repeated = JumpPeak(true);
                Check(peak > .08f && peak < .16f, "jump height");
                Check(Mathf.Abs(peak - repeated) < .005f, "air jump refusal");
                Settle();
                for (int i = 0; i < 600; ++i) motor.Step(0, 1, false, false, 1f/60);
                Check(Mathf.Abs(motor.transform.position.z) < 3 && Mathf.Abs(motor.transform.position.x) < 3,
                    "room boundary collision");
                motor.ReturnToStart();
                Check(Vector3.Distance(motor.transform.position, expectedSpawn) < .001f, "return to spawn");
                string result = "PASS: actual CharacterController with production Step; grounding, walk/run/diagonal, " +
                    "30/60 movement steps, jump/landing, air-jump refusal, boundary, return.\n" +
                    "Walk distance=" + cardinal.ToString("F5") + "m; jump peak=" + peak.ToString("F5") + "m.\n" +
                    "Synchronous physics smoke, not a keyboard/device, frame-loop, long stability or performance test.\n";
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MotorSmokeTests).Assembly).resolvedPath;
                string folder = Path.Combine(Directory.GetParent(package).Parent.Parent.FullName, "Artifacts", "UnityEditor");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, motor.gameObject.scene.path==BedsideBearBuilder.ScenePath?"bedside-bear-motor-smoke.txt":motor.gameObject.scene.path==BearFitBuilder.ScenePath?"bear-fit-motor-smoke.txt":motor.gameObject.scene.path==FleeceStudyBuilder.ScenePath?"fleece-motor-smoke.txt":(motor.gameObject.scene.path==BeddingRefinementBuilder.ScenePath || motor.gameObject.scene.path==BeddingContactBuilder.ScenePath || motor.gameObject.scene.path==BeddingDrapeBuilder.ScenePath || motor.gameObject.scene.path==BeddingSurfaceBuilder.ScenePath || motor.gameObject.scene.path==BeddingRoundBuilder.ScenePath || motor.gameObject.scene.path==BeddingStableBuilder.ScenePath)?"bedding-motor-smoke.txt":bedside?"bedside-motor-smoke.txt":twilight?"twilight-motor-smoke.txt":m2?"m2-motor-smoke.txt":"motor-smoke.txt"), "Scene="+motor.gameObject.scene.path+"\n"+result);
                Debug.Log("SORAJIRO MOTOR TESTS: " + result);
            }
            finally
            {
                motor.ReturnToStart();
                if (motor.visualRoot) motor.visualRoot.rotation = visualRotation;
            }
        }

        private static void Check(bool ok, string name)
        {
            if (!ok) throw new Exception("Motor smoke failed: " + name);
        }
    }
}
