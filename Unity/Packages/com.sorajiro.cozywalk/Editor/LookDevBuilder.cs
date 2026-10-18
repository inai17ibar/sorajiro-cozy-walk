using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class LookDevBuilder
    {
        private static string HdrpVersion => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;

        [MenuItem("Sorajiro/1. Preflight (no asset writes)")]
        public static void Preflight()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play mode and wait for compilation.");
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                throw new InvalidOperationException("An active HDRP pipeline asset is required.");
            if (Application.unityVersion != "6000.6.5f1" || HdrpVersion != "17.6.0")
                throw new InvalidOperationException("This initial revision targets observed Unity 6000.6.5f1 / HDRP 17.6.0. Review before changing versions.");
            if (!Shader.Find("HDRP/Lit")) throw new InvalidOperationException("HDRP/Lit shader not found.");
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your edited scene explicitly first.");
            string folder = Path.GetFullPath(GeneratedContentGuard.Output);
            RejectLinks(folder);
            bool reuse = GeneratedContentGuard.CanReuse(folder, Application.unityVersion, HdrpVersion);
            Debug.Log("SORAJIRO PREFLIGHT: " + (reuse ? "REUSE; no changes" : "CREATE new placeholder assets") +
                      "; " + GeneratedContentGuard.Output + "; shader/render/performance are NOT verified.");
        }

        [MenuItem("Sorajiro/2. Create LookDev scene (new revision only)")]
        public static void Create()
        {
            Preflight();
            string folder = Path.GetFullPath(GeneratedContentGuard.Output);
            if (GeneratedContentGuard.CanReuse(folder, Application.unityVersion, HdrpVersion)) return;
            // Additive creation protects the owner's currently open scene and scene setup.
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            bool saved = false;
            try
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                Material blue = Material("BlueCloth_PREVIEW", new Color(.28f,.63f,.82f), .1f);
                Material white = Material("WhiteApplique_PREVIEW", new Color(.86f,.86f,.81f), .12f);
                Material black = Material("SafetyPlastic_PREVIEW", new Color(.008f,.009f,.012f), .85f);
                Material wood = Material("Wood_PREVIEW", new Color(.43f,.27f,.14f), .3f);
                Material plaster = Material("Plaster_PREVIEW", new Color(.7f,.65f,.54f), .1f);
                Material rug = Material("Rug_PREVIEW", new Color(.54f,.56f,.54f), .1f);

                Cube("Floor_PLACEHOLDER", new Vector3(0,-.05f,0), new Vector3(6,.1f,6), wood);
                Cube("BackWall", new Vector3(0,1.25f,3), new Vector3(6,.1f,2.5f), plaster).transform.rotation = Quaternion.Euler(90,0,0);
                Cube("LeftWall", new Vector3(-3,1.25f,0), new Vector3(.1f,2.5f,6), plaster);
                // Window aperture on +X, rather than a luminous image used as scenery.
                Cube("WindowWall_Lower", new Vector3(3,.35f,0), new Vector3(.12f,.7f,6), plaster);
                Cube("WindowWall_Upper", new Vector3(3,2.3f,0), new Vector3(.12f,.4f,6), plaster);
                Cube("WindowWall_Front", new Vector3(3,1.4f,-2), new Vector3(.12f,1.4f,2), plaster);
                Cube("WindowWall_Back", new Vector3(3,1.4f,2), new Vector3(.12f,1.4f,2), plaster);
                Cube("WindowSill_PREVIEW", new Vector3(2.9f,.72f,0), new Vector3(.25f,.06f,2.1f), wood);
                Cube("WindowFrame_PREVIEW", new Vector3(2.95f,1.4f,0), new Vector3(.08f,1.4f,.06f), wood);
                Cube("Rug_PLACEHOLDER_NO_PILE", new Vector3(0,.006f,0), new Vector3(1.8f,.012f,1.6f), rug);
                Cube("JumpBlock", new Vector3(.75f,.06f,.55f), new Vector3(.3f,.12f,.4f), wood);
                // Low invisible rails close the otherwise open test area.
                InvisibleRail(new Vector3(0,.15f,-3), new Vector3(6,.3f,.1f));

                GameObject player = new GameObject("Sorajiro_M1_PLACEHOLDER_NO_FUR_NO_RIG");
                player.layer = 2; // Ignore Raycast: camera sweep ignores its own player.
                player.transform.position = new Vector3(0,.016f,0);
                CharacterController cc = player.AddComponent<CharacterController>();
                cc.height = .38f; cc.radius = .09f; cc.center = new Vector3(0,.19f,0);
                cc.stepOffset = .04f; cc.skinWidth = .005f; cc.minMoveDistance = 0;
                SorajiroMotor motor = player.AddComponent<SorajiroMotor>();
                GameObject visual = new GameObject("PrimitiveSilhouette_ONLY");
                visual.transform.SetParent(player.transform, false);
                Bear(visual.transform, blue, white, black);
                motor.visualRoot = visual.transform;
                Camera camera = Camera("GameplayCamera", new Vector3(0,.55f,-1.2f), new Vector3(0,.25f,0), 60);
                camera.enabled = true; camera.tag = "MainCamera";
                OrbitCamera orbit = camera.gameObject.AddComponent<OrbitCamera>();
                orbit.target = motor; motor.view = camera.transform;

                Light key = Light("WindowSun_PREVIEW", LightType.Directional, new Vector3(2,2,1), 6000, new Color(1,.87f,.68f));
                key.transform.rotation = Quaternion.Euler(38,-65,0);
                key.GetComponent<HDAdditionalLightData>().angularDiameter = 3;
                Light fill = Light("SoftFill_PREVIEW", LightType.Point, new Vector3(-1,1,-1), 35, new Color(.75f,.84f,1));
                fill.range = 6; fill.shadows = LightShadows.None;
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.Add<Exposure>().mode.Override(ExposureMode.Fixed);
                profile.components.OfType<Exposure>().Single().fixedExposure.Override(9);
                var sky = profile.Add<GradientSky>();
                sky.top.Override(new Color(.35f,.46f,.6f));
                sky.middle.Override(new Color(.6f,.65f,.7f));
                sky.bottom.Override(new Color(.25f,.23f,.2f));
                var environment = profile.Add<VisualEnvironment>();
                environment.skyType.Override(SkySettings.GetUniqueID<GradientSky>());
                profile.Add<DepthOfField>().active = false;
                profile.Add<MotionBlur>().active = false;
                profile.Add<Bloom>().intensity.Override(0);
                AssetDatabase.CreateAsset(profile, GeneratedContentGuard.Output + "/LookDevVolume.asset");
                var volume = new GameObject("FixedExposure_EV9_NO_DOF").AddComponent<Volume>();
                volume.isGlobal = true; volume.sharedProfile = profile;

                Camera("S01_Portrait", new Vector3(0,.35f,.95f), new Vector3(0,.29f,0), 50);
                Camera("S02_Gameplay", new Vector3(0,.5f,-1.45f), new Vector3(0,.24f,.3f), 60);
                Camera("S03_ThreeQuarter", new Vector3(.85f,.55f,.85f), new Vector3(0,.24f,0), 55);
                Camera("S04_Contact", new Vector3(.35f,.12f,.45f), new Vector3(0,.05f,0), 50);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, GeneratedContentGuard.ScenePath))
                    throw new IOException("Scene save failed; partial revision preserved.");
                saved = true;
                AssetDatabase.Refresh();
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(GeneratedContentGuard.ScenePath))
                    throw new IOException("Saved scene cannot be loaded as an asset.");
                GeneratedContentGuard.WriteReceipt(folder, Application.unityVersion, HdrpVersion);
                AssetDatabase.Refresh();
                Debug.Log("SORAJIRO ASSETS: saved placeholder scene/materials/volume. Not reference quality. " + GeneratedContentGuard.ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                // Only close the scene created here; never close the owner's open scenes.
                if (saved) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Sorajiro/3. Open generated LookDev scene")]
        public static void Open()
        {
            Preflight();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(GeneratedContentGuard.ScenePath))
                throw new InvalidOperationException("Create the scene first.");
            EditorSceneManager.OpenScene(GeneratedContentGuard.ScenePath, OpenSceneMode.Single);
        }

        private static void RejectLinks(string path)
        {
            for (DirectoryInfo d = new DirectoryInfo(path); d != null; d = d.Parent)
                if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Symlink destination refused.");
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            var result = new Material(Shader.Find("HDRP/Lit")) { name = name };
            result.SetColor("_BaseColor", color); result.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(result, GeneratedContentGuard.Output + "/" + name + ".mat");
            return result;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static void InvisibleRail(Vector3 position, Vector3 scale)
        {
            var obj = new GameObject("InvisibleBoundary"); obj.transform.position = position;
            obj.AddComponent<BoxCollider>().size = scale;
        }

        private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name; obj.layer = 2; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Bear(Transform root, Material blue, Material white, Material black)
        {
            Part(root,"Body",new Vector3(0,.16f,0),new Vector3(.18f,.22f,.14f),blue);
            Part(root,"Head",new Vector3(0,.30f,0),new Vector3(.22f,.19f,.16f),blue);
            foreach (int sign in new[]{-1,1})
            {
                Part(root,"Ear",new Vector3(sign*.081f,.374f,0),new Vector3(.075f,.075f,.04f),blue);
                Part(root,"InnerEar",new Vector3(sign*.081f,.374f,.021f),new Vector3(.048f,.048f,.012f),white);
                Part(root,"Arm",new Vector3(sign*.102f,.17f,0),new Vector3(.047f,.13f,.05f),blue);
                Part(root,"Foot",new Vector3(sign*.052f,.038f,.017f),new Vector3(.068f,.076f,.083f),blue);
                Part(root,"SafetyEye",new Vector3(sign*.048f,.311f,.073f),new Vector3(.021f,.025f,.014f),black);
            }
            Part(root,"Muzzle",new Vector3(0,.277f,.075f),new Vector3(.105f,.075f,.04f),white);
            Part(root,"SafetyNose",new Vector3(0,.293f,.097f),new Vector3(.035f,.022f,.015f),black);
            Part(root,"Tail",new Vector3(0,.105f,-.076f),new Vector3(.037f,.037f,.037f),blue);
            // Appliqué silhouette placeholders, not a UV/material reconstruction.
            Part(root,"CloudCenter",new Vector3(.035f,.352f,.066f),new Vector3(.04f,.028f,.012f),white);
            Part(root,"CloudLobe",new Vector3(.02f,.348f,.068f),new Vector3(.03f,.021f,.012f),white);
            Part(root,"CloudLobe",new Vector3(.05f,.348f,.068f),new Vector3(.03f,.021f,.012f),white);
        }

        private static Camera Camera(string name, Vector3 position, Vector3 lookAt, float fov)
        {
            var obj = new GameObject(name); obj.transform.position = position; obj.transform.LookAt(lookAt);
            var camera = obj.AddComponent<Camera>(); camera.fieldOfView = fov;
            camera.nearClipPlane = .03f; camera.farClipPlane = 30; camera.enabled = false;
            obj.AddComponent<HDAdditionalCameraData>();
            return camera;
        }

        private static Light Light(string name, LightType type, Vector3 position, float intensity, Color color)
        {
            var obj = new GameObject(name); obj.transform.position = position;
            var light = obj.AddComponent<Light>(); light.type = type;
            obj.AddComponent<HDAdditionalLightData>();
            light.intensity = intensity; light.color = color; light.shadows = LightShadows.Soft;
            return light;
        }
    }
}
