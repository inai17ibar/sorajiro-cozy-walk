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
    public static class M2LookDevBuilder
    {
        public const string Output = "Assets/Sorajiro/Generated/M2_v003";
        public const string ScenePath = Output + "/L_LookDev.unity";
        public static string Repository => Directory.GetParent(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(M2LookDevBuilder).Assembly).resolvedPath).Parent.Parent.FullName;
        private static string Source => Path.Combine(Repository,"Artifacts/SourceArt/Bear/m2_v002");
        private static string Hdrp => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe => "M2-v003-" + GeneratedContentGuard.HashFile(Path.Combine(Source,"receipt.json")) +
            GeneratedContentGuard.HashFile(Path.Combine(Repository,"Unity/Packages/com.sorajiro.cozywalk/Editor/M2LookDevBuilder.cs"));

        [MenuItem("Sorajiro/M2/1. Preflight bear and scene (no writes)")]
        public static void Preflight()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play and wait for compilation.");
            if (Application.unityVersion!="6000.6.5f1" || Hdrp!="17.6.0" || !(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                throw new InvalidOperationException("Reviewed Unity 6000.6.5f1 / HDRP 17.6.0 required.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your scene edits explicitly first.");
            foreach(string path in new[]{Path.GetFullPath(Output),Source})
                for(DirectoryInfo d=new DirectoryInfo(path);d!=null;d=d.Parent)
                    if(d.Exists && (d.Attributes & FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("Symlink destination/source refused.");
            string receipt=File.ReadAllText(Path.Combine(Source,"receipt.json"));
            foreach(string name in new[]{"Sorajiro_Bear_M2.blend","Sorajiro_Bear_M2.fbx","FleeceMicroNormal.png"})
            {
                string path=Path.Combine(Source,name);
                if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("Symlink source refused.");
                string hash=GeneratedContentGuard.HashFile(path);
                if(!receipt.Contains("\""+name+"\": \""+hash+"\"")) throw new InvalidOperationException("Source art changed since generation; preserve and make new revision.");
                if(System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path).Take(128).ToArray()).StartsWith("version https://git-lfs.github.com/spec/v1"))
                    throw new InvalidOperationException("LFS pointer is not an asset.");
            }
            bool reuse=GeneratedContentGuard.CanReuse(Path.GetFullPath(Output),Application.unityVersion,Hdrp,Recipe);
            Debug.Log("SORAJIRO M2 PREFLIGHT: "+(reuse?"REUSE, no writes":"CREATE new art trial")+"; "+Output+"; full fur/animation/quality/performance not approved.");
        }

        [MenuItem("Sorajiro/M2/2. Create bear lookdev (new revision)")]
        public static void Create()
        {
            Preflight();
            string folder=Path.GetFullPath(Output);
            if(GeneratedContentGuard.CanReuse(folder,Application.unityVersion,Hdrp,Recipe)) return;
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                File.Copy(Path.Combine(Source,"Sorajiro_Bear_M2.fbx"),Path.Combine(folder,"Sorajiro_Bear_M2.fbx"),false);
                File.Copy(Path.Combine(Source,"FleeceMicroNormal.png"),Path.Combine(folder,"FleeceMicroNormal.png"),false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                string normalPath=Output+"/FleeceMicroNormal.png";
                var ti=(TextureImporter)AssetImporter.GetAtPath(normalPath);
                ti.textureType=TextureImporterType.NormalMap; ti.sRGBTexture=false; ti.wrapMode=TextureWrapMode.Repeat; ti.SaveAndReimport();
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                Material blue=Mat("BlueFleece",new Color(.38f,.68f,.87f),.1f,normal);
                Material white=Mat("WhiteApplique",new Color(.88f,.87f,.82f),.08f,normal);
                Material black=Mat("SafetyPlastic",new Color(.004f,.006f,.009f),.88f);
                Material stitch=Mat("Stitch",new Color(.24f,.46f,.58f),.05f);
                Material fur=Mat("FurPatchTrial",new Color(.38f,.68f,.87f),.04f);
                fur.SetFloat("_DoubleSidedEnable",1); fur.EnableKeyword("_DOUBLESIDED_ON");
                string modelPath=Output+"/Sorajiro_Bear_M2.fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.importAnimation=false; importer.optimizeGameObjects=false;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                foreach(var material in new[]{blue,white,black,stitch,fur})
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);
                importer.SaveAndReimport();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                var player=new GameObject("Sorajiro_M2_ART_TRIAL");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,player.transform);
                visual.name="Bear_M2_EDITABLE_RIG_TRIAL";
                foreach(var tr in visual.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer=2;
                var renderers=visual.GetComponentsInChildren<Renderer>();
                Bounds bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                var marker=visual.GetComponentsInChildren<Transform>().Single(t=>t.name=="FaceForward_UnityCheck");
                if(bounds.size.y<.37f || bounds.size.y>.43f || marker.position.z<.08f || Mathf.Abs(marker.position.x)>.01f)
                    throw new InvalidOperationException("FBX scale/axes invalid: "+bounds.size+" face="+marker.position);
                if(visual.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=2)
                    throw new InvalidOperationException("Expected consolidated skinned body + short-pile patch.");
                float lowest=float.PositiveInfinity;
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var baked=new Mesh(); skin.BakeMesh(baked);
                    foreach(var vertex in baked.vertices) lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y);
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                visual.transform.localPosition=Vector3.up*(-lowest-.005f);
                player.transform.position=new Vector3(0,.025f,0); player.layer=2;
                var cc=player.AddComponent<CharacterController>(); cc.height=.4f; cc.radius=.085f; cc.center=Vector3.up*.2f;
                cc.stepOffset=.04f; cc.skinWidth=.005f; cc.minMoveDistance=0;
                var motor=player.AddComponent<SorajiroMotor>(); motor.visualRoot=visual.transform;
                motor.diagnosticLabel="M2 制作モデル試作：背面に短毛パッチ／歩行アニメーション未実装";

                Material wall=Mat("WarmPlaster",new Color(.76f,.73f,.65f),.05f);
                Material frame=Mat("WindowFrame",new Color(.84f,.81f,.73f),.3f);
                Material wood=Mat("OakBoard_Procedural",Color.white,.28f);
                wood.SetTexture("_BaseColorMap",WoodTexture()); wood.SetTextureScale("_BaseColorMap",new Vector2(2,8));
                Material rug=Mat("RugWoven_SURFACE_NO_PILE",new Color(.67f,.70f,.69f),.04f,normal);
                Cube("FloorCollision",new Vector3(0,-.06f,0),new Vector3(5,.1f,5),null);
                for(int row=0;row<3;row++) for(int board=0;board<22;board++)
                {
                    var plank=Cube("OakPlank",new Vector3(-2.42f+board*.23f,-.012f,-1.67f+row*1.67f),new Vector3(.228f,.022f,1.668f),wood);
                    UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>());
                }
                Cube("Rug_WOVEN_TRIAL_NO_PILE",new Vector3(0,.01f,0),new Vector3(1.45f,.02f,1.25f),rug);
                Cube("BackWall",new Vector3(0,1.2f,2.5f),new Vector3(5,2.4f,.10f),wall);
                Cube("LeftWall",new Vector3(-2.5f,1.2f,0),new Vector3(.1f,2.4f,5),wall);
                Cube("WindowLower",new Vector3(2.5f,.3f,0),new Vector3(.12f,.6f,5),wall);
                Cube("WindowUpper",new Vector3(2.5f,2.25f,0),new Vector3(.12f,.3f,5),wall);
                Cube("WindowFront",new Vector3(2.5f,1.35f,-1.5f),new Vector3(.12f,1.5f,2),wall);
                Cube("WindowBack",new Vector3(2.5f,1.35f,2f),new Vector3(.12f,1.5f,1),wall);
                Cube("WindowSill",new Vector3(2.4f,.61f,.5f),new Vector3(.32f,.06f,2.1f),frame);
                Cube("WindowMullion",new Vector3(2.45f,1.35f,.5f),new Vector3(.08f,1.5f,.045f),frame);
                Cube("WindowCrossbar",new Vector3(2.45f,1.4f,.5f),new Vector3(.08f,.045f,2),frame);
                Cube("InvisibleBoundary",new Vector3(0,.15f,-2.5f),new Vector3(5,.3f,.1f),null);
                Cube("JumpBlock",new Vector3(.85f,.06f,.45f),new Vector3(.3f,.12f,.35f),wood);

                var gameplay=Cam("GameplayCamera",new Vector3(0,.5f,-1.2f),new Vector3(0,.25f,0),55);
                gameplay.enabled=true; gameplay.tag="MainCamera";
                gameplay.gameObject.AddComponent<OrbitCamera>().target=motor; motor.view=gameplay.transform;
                Cam("S01_Portrait",new Vector3(0,.30f,.56f),new Vector3(0,.284f,0),38);
                Cam("S02_Gameplay",new Vector3(0,.48f,-1.15f),new Vector3(0,.23f,.15f),48);
                Cam("S03_ThreeQuarter",new Vector3(.45f,.30f,.58f),new Vector3(0,.205f,0),38);
                Cam("S04_Contact",new Vector3(.21f,.11f,.32f),new Vector3(0,.035f,0),36);
                Cam("S05_PilePatch_Diagnostic",new Vector3(0,.30f,-.19f),new Vector3(0,.30f,0),35)
                    .GetComponent<HDAdditionalCameraData>().antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                var sun=Light("WindowSun",LightType.Directional,new Vector3(2,2,1),3000,new Color(1,.96f,.88f));
                sun.lightUnit=LightUnit.Lux; sun.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.35f,-.3f));
                sun.GetComponent<HDAdditionalLightData>().angularDiameter=5;
                var fill=Light("WindowArea_FILL_PREVIEW",LightType.Rectangle,new Vector3(2.2f,1.3f,.5f),8000,new Color(1,.97f,.93f));
                fill.lightUnit=LightUnit.Lumen; fill.intensity=8000; fill.areaSize=new Vector2(2,1.5f);
                fill.transform.LookAt(new Vector3(0,.2f,0)); fill.shadows=LightShadows.None;
                var bounce=Light("RoomBounce_FILL_PREVIEW",LightType.Point,new Vector3(-.7f,.9f,-.6f),1800,new Color(.88f,.93f,1));
                bounce.lightUnit=LightUnit.Lumen; bounce.intensity=1800; bounce.range=5; bounce.shadows=LightShadows.None;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                profile.Add<Exposure>().mode.Override(ExposureMode.Fixed); profile.components.OfType<Exposure>().Single().fixedExposure.Override(8);
                var sky=profile.Add<GradientSky>(); sky.top.Override(new Color(.45f,.57f,.72f)); sky.middle.Override(new Color(.72f,.76f,.80f)); sky.bottom.Override(new Color(.30f,.28f,.23f));
                sky.exposure.Override(9); sky.updateMode.Override(EnvironmentUpdateMode.OnChanged);
                var environment=profile.Add<VisualEnvironment>(); environment.skyType.Override(SkySettings.GetUniqueID<GradientSky>()); environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
                profile.Add<VolumetricClouds>().enable.Override(false); profile.Add<CloudLayer>().opacity.Override(0);
                profile.Add<DepthOfField>().focusMode.Override(DepthOfFieldMode.Off); profile.Add<MotionBlur>().intensity.Override(0); profile.Add<Bloom>().intensity.Override(0);
                var ao=profile.Add<ScreenSpaceAmbientOcclusion>(); ao.intensity.Override(.55f); ao.radius.Override(.25f);
                AssetDatabase.CreateAsset(profile,Output+"/LookDevVolume.asset");
                foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
                var volume=new GameObject("LookDev_FixedEV8_NO_DOF_NO_BLUR").AddComponent<Volume>(); volume.isGlobal=true; volume.priority=100; volume.sharedProfile=profile;
                AssetDatabase.SaveAssets();
                if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Scene save failed; partial output preserved.");
                AssetDatabase.Refresh();
                GeneratedContentGuard.WriteReceipt(folder,Application.unityVersion,Hdrp,Recipe); AssetDatabase.Refresh();
                Debug.Log("SORAJIRO M2 ASSETS: real FBX imported; height="+bounds.size.y+"m; face +Z; 2 SkinnedMeshRenderers; generic rig trial; full fur/animation not created. "+ScenePath);
            }
            finally
            {
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
            }
        }

        [MenuItem("Sorajiro/M2/3. Open bear lookdev")]
        public static void Open()
        {
            Preflight();
            if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new InvalidOperationException("Create M2 first.");
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }

        private static Material Mat(string name,Color color,float smoothness,Texture2D normal=null)
        {
            var m=new Material(Shader.Find("HDRP/Lit")){name=name}; m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smoothness);
            if(normal){m.SetTexture("_NormalMap",normal); m.SetTextureScale("_BaseColorMap",new Vector2(12,12)); m.SetFloat("_NormalScale",.18f); m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_NORMALMAP_TANGENT_SPACE");}
            AssetDatabase.CreateAsset(m,Output+"/"+name+".mat"); return m;
        }
        private static Texture2D WoodTexture()
        {
            var texture=new Texture2D(256,512,TextureFormat.RGB24,false);
            for(int y=0;y<512;y++) for(int x=0;x<256;x++)
            {
                float grain=Mathf.PerlinNoise(x*.09f+Mathf.Sin(y*.018f)*2,y*.004f);
                float fine=Mathf.PerlinNoise(x*.65f,y*.018f);
                texture.SetPixel(x,y,Color.Lerp(new Color(.49f,.36f,.23f),new Color(.57f,.43f,.29f),grain*.85f+fine*.15f));
            }
            texture.Apply(); string path=Output+"/OakGrain_original_procedural.png"; File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static GameObject Cube(string name,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.position=position; obj.transform.localScale=scale;
            if(material) obj.GetComponent<Renderer>().sharedMaterial=material; else UnityEngine.Object.DestroyImmediate(obj.GetComponent<Renderer>());
            return obj;
        }
        private static Camera Cam(string name,Vector3 position,Vector3 target,float fov)
        {
            var obj=new GameObject(name); obj.transform.position=position; obj.transform.LookAt(target); var cam=obj.AddComponent<Camera>();
            cam.fieldOfView=fov; cam.nearClipPlane=.02f; cam.farClipPlane=25; cam.enabled=false;
            var hd=obj.AddComponent<HDAdditionalCameraData>(); hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            hd.allowDynamicResolution=false; hd.allowDeepLearningSuperSampling=false; hd.allowFidelityFX2SuperResolution=false;
            return cam;
        }
        private static Light Light(string name,LightType type,Vector3 position,float intensity,Color color)
        {
            var obj=new GameObject(name); obj.transform.position=position; var light=obj.AddComponent<Light>(); light.type=type;
            obj.AddComponent<HDAdditionalLightData>(); light.intensity=intensity; light.color=color; light.shadows=LightShadows.Soft; return light;
        }
    }
}
