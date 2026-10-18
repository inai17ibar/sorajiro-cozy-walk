using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sorajiro.Editor
{
    public static class VerificationTools
    {
        private static string EvidenceFolder
        {
            get
            {
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(VerificationTools).Assembly).resolvedPath;
                string root = Directory.GetParent(package).Parent.Parent.FullName;
                string result = Path.Combine(root, "Artifacts", "UnityEditor");
                Directory.CreateDirectory(result);
                return result;
            }
        }

        [MenuItem("Sorajiro/4. Log runtime status (Play)")]
        public static void RuntimeStatus()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var motor = UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();
            if (!motor) throw new InvalidOperationException("Sorajiro player not found.");
            var camera = Camera.main;
            string json = JsonUtility.ToJson(new RuntimeEvidence {
                utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                scenePath=motor.gameObject.scene.path,
                assetState=motor.gameObject.scene.path==TwilightLookDevBuilder.ScenePath?
                    "master_bear_full_body_pile_rig_animation_twilight_room_trial":motor.gameObject.scene.path==M2LookDevBuilder.ScenePath?
                    "generated_model_uv_generic_rig_back_head_pile_patch_no_animation":"generated_placeholder_no_fur_no_rig_no_animation",
                gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                position = motor.transform.position, grounded = motor.GetComponent<CharacterController>().isGrounded,
                paused = motor.Paused, focused = motor.Focused,
                outputWidth = Screen.width, outputHeight = Screen.height,
                fieldOfViewVertical = camera ? camera.fieldOfView : 0,
                cameraPosition = camera ? camera.transform.position : Vector3.zero,
                cameraEuler = camera ? camera.transform.eulerAngles : Vector3.zero
            }, true);
            string path = Path.Combine(EvidenceFolder, "runtime-" + DateTime.UtcNow.Ticks + ".json");
            File.WriteAllText(path, json + "\n");
            Debug.Log("SORAJIRO RUNTIME: " + json + "\nEvidence: " + path);
        }

        [MenuItem("Sorajiro/5. Capture Game view (Play, editor baseline)")]
        public static void Capture()
        {
            RuntimeStatus();
            string path = Path.Combine(EvidenceFolder, "current-editor-" + DateTime.UtcNow.Ticks + ".png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log("SORAJIRO CAPTURE requested for next frame: " + path + "; verify file and actual asset state; Editor output only.");
        }

        [MenuItem("Sorajiro/6. Test generated-content protection (Edit)")]
        public static void GuardTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            string fixture = Path.Combine(EvidenceFolder, "guard-fixture-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fixture);
            try
            {
                if (GeneratedContentGuard.CanReuse(fixture, "test", "test")) throw new Exception("Empty directory must be new.");
                // Test fixture only; never a real Unity scene. Not imported by AssetDatabase.
                string asset = Path.Combine(fixture, "L_LookDev.unity");
                File.WriteAllText(asset, "test fixture; not an asset");
                ExpectRefusal(() => GeneratedContentGuard.CanReuse(fixture, "test", "test"));
                GeneratedContentGuard.WriteReceipt(fixture, "test", "test");
                if (!GeneratedContentGuard.CanReuse(fixture, "test", "test")) throw new Exception("Unchanged receipt must reuse.");
                ExpectRefusal(() => GeneratedContentGuard.CanReuse(fixture, "changed", "test"));
                ExpectRefusal(() => GeneratedContentGuard.WriteReceipt(fixture, "test", "test"));
                File.AppendAllText(asset, "owner edit");
                ExpectRefusal(() => GeneratedContentGuard.CanReuse(fixture, "test", "test"));
                string result = "PASS: 6 actual Unity JsonUtility/receipt checks: empty, unowned, reuse, version, receipt overwrite, owner edit.\n";
                File.WriteAllText(Path.Combine(EvidenceFolder, "guard-tests.txt"), result);
                Debug.Log("SORAJIRO GUARD TESTS: " + result);
            }
            finally { Directory.Delete(fixture, true); } // Only the newly created private fixture.
        }

        private static void ExpectRefusal(Action action)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected protection refusal.");
        }

        [Serializable] private sealed class RuntimeEvidence
        {
            public string utc, unity, gpu, graphicsApi,scenePath;
            public Vector3 position, cameraPosition, cameraEuler;
            public bool grounded, paused, focused;
            public int outputWidth, outputHeight;
            public float fieldOfViewVertical;
            public string scope = "Editor Play snapshot; not a Player build, fixed-shot comparison, motion clip or performance benchmark";
            public string assetState = "generated_placeholder_no_fur_no_rig_no_animation";
            public string internalResolution = "unverified";
            public string upscaler = "unverified";
        }
    }
}
