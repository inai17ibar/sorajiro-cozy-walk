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
    // A separate, reproducible art trial. Earlier scenes and owner edits are preserved.
    public static class TwilightLookDevBuilder
    {
        public const string Output = "Assets/Sorajiro/Generated/Twilight_v010";
        public const string ScenePath = Output + "/L_LookDev.unity";
        public static string Repository => M2LookDevBuilder.Repository;
        private static string BearSource => Path.Combine(Repository,"Artifacts/SourceArt/Bear/twilight_v006");
        private static string RoomSource => Path.Combine(Repository,"Artifacts/SourceArt/Room/twilight_v007");
        private static string Hdrp => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe => "twilight-v010-" + GeneratedContentGuard.HashFile(Path.Combine(BearSource,"receipt.json")) +
            GeneratedContentGuard.HashFile(Path.Combine(RoomSource,"receipt.json")) +
            GeneratedContentGuard.HashFile(Path.Combine(Repository,"Unity/Packages/com.sorajiro.cozywalk/Editor/TwilightLookDevBuilder.cs")) +
            GeneratedContentGuard.HashFile(Path.Combine(Repository,"Unity/Packages/com.sorajiro.cozywalk/Editor/BearAnimationBuilder.cs")) +
            GeneratedContentGuard.HashFile(Path.Combine(Repository,"Unity/Packages/com.sorajiro.cozywalk/Editor/TwilightSurfaceBuilder.cs")) +
            GeneratedContentGuard.HashFile(Path.Combine(Repository,"Unity/Packages/com.sorajiro.cozywalk/Runtime/PlushGait.cs"));

        [MenuItem("Sorajiro/Twilight/1. Preflight (no writes)")]
        public static void Preflight()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stop Play and wait for compilation.");
            if(Application.unityVersion!="6000.6.5f1" || Hdrp!="17.6.0" || !(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                throw new InvalidOperationException("Reviewed Unity 6000.6.5f1 / HDRP 17.6.0 required.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your scene edits explicitly first.");
            foreach(string path in new[]{Path.GetFullPath(Output),BearSource,RoomSource})
                for(DirectoryInfo d=new DirectoryInfo(path);d!=null;d=d.Parent)
                    if(d.Exists && (d.Attributes & FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("Symlink source/destination refused.");
            CheckSource(BearSource,new[]{"Sorajiro_Bear_M2.blend","Sorajiro_Bear_M2.fbx","FleeceMicroNormal.png"});
            CheckSource(RoomSource,new[]{"TwilightRoom.blend","TwilightRoom.fbx"});
            Debug.Log("SORAJIRO TWILIGHT PREFLIGHT: "+(GeneratedContentGuard.CanReuse(Path.GetFullPath(Output),Application.unityVersion,Hdrp,Recipe)?"REUSE":"CREATE new revision")+"; "+Output);
        }

        private static void CheckSource(string folder,string[] names)
        {
            string receipt=File.ReadAllText(Path.Combine(folder,"receipt.json"));
            foreach(string name in names)
            {
                string path=Path.Combine(folder,name);
                if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0 || !receipt.Contains("\""+name+"\": \""+GeneratedContentGuard.HashFile(path)+"\""))
                    throw new InvalidOperationException("Source changed; preserve and create a new art revision: "+name);
                using(var stream=File.OpenRead(path)) using(var reader=new StreamReader(stream))
                { char[] prefix=new char[48]; reader.Read(prefix,0,prefix.Length); if(new string(prefix).StartsWith("version https://git-lfs.github.com/spec/v1")) throw new IOException("LFS pointer is not an asset."); }
            }
        }

        [MenuItem("Sorajiro/Twilight/2. Create master bear and twilight room")]
        public static void Create()
        {
            Preflight(); string folder=Path.GetFullPath(Output);
            if(GeneratedContentGuard.CanReuse(folder,Application.unityVersion,Hdrp,Recipe)) return;
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                Directory.CreateDirectory(folder);
                File.Copy(Path.Combine(BearSource,"Sorajiro_Bear_M2.fbx"),Path.Combine(folder,"Bear.fbx"),false);
                File.Copy(Path.Combine(BearSource,"FleeceMicroNormal.png"),Path.Combine(folder,"FleeceMicroNormal.png"),false);
                File.Copy(Path.Combine(RoomSource,"TwilightRoom.fbx"),Path.Combine(folder,"Room.fbx"),false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var ti=(TextureImporter)AssetImporter.GetAtPath(Output+"/FleeceMicroNormal.png");
                ti.textureType=TextureImporterType.NormalMap; ti.sRGBTexture=false; ti.wrapMode=TextureWrapMode.Repeat; ti.SaveAndReimport();
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/FleeceMicroNormal.png");
                Material[] bearMaterials={
                    Mat("BlueFleece",new Color(.29f,.63f,.82f),.08f,normal),
                    Mat("WhiteApplique",new Color(.87f,.86f,.82f),.06f,normal),
                    Mat("SafetyPlastic",new Color(.025f,.032f,.040f),.86f),
                    Mat("Stitch",new Color(.15f,.36f,.46f),.04f),
                    Mat("PileBlue",new Color(.29f,.63f,.82f),0,null,true),
                    Mat("PileWhite",new Color(.87f,.86f,.82f),0,null,true)};
                ImportModel(Output+"/Bear.fbx",bearMaterials,true);
                var player=new GameObject("Sorajiro_MASTER_BEAR_ART_TRIAL"); player.layer=2;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Bear.fbx"),player.transform);
                visual.name="MasterBear_EDITABLE_GENERIC_RIG";
                foreach(var tr in visual.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer=2;
                var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
                if(skins.Length!=2) throw new Exception("Expected cloth + full-body pile skins.");
                Bounds bounds=skins[0].bounds; foreach(var r in skins) bounds.Encapsulate(r.bounds);
                Transform face=visual.GetComponentsInChildren<Transform>().Single(t=>t.name=="FaceForward_UnityCheck");
                if(bounds.size.y<.37f || bounds.size.y>.43f || face.position.z<.08f) throw new Exception("Bear scale/axis mismatch: "+bounds.size+" face="+face.position);
                float min=float.PositiveInfinity;
                foreach(var skin in skins)
                {
                    skin.updateWhenOffscreen=true; skin.shadowCastingMode=ShadowCastingMode.On;
                    var mesh=new Mesh(); skin.BakeMesh(mesh);
                    foreach(var p in mesh.vertices) min=Mathf.Min(min,skin.transform.TransformPoint(p).y);
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                visual.transform.localPosition=Vector3.up*(-min-.005f);
                visual.transform.localRotation=Quaternion.Euler(0,180,0);
                player.transform.position=new Vector3(-.12f,.030f,-.50f);
                var cc=player.AddComponent<CharacterController>(); cc.height=.4f; cc.radius=.085f; cc.center=Vector3.up*.2f; cc.stepOffset=.04f; cc.skinWidth=.005f; cc.minMoveDistance=0;
                var motor=player.AddComponent<SorajiroMotor>(); motor.visualRoot=visual.transform;
                motor.walkSpeed=.28f; motor.runSpeed=.52f;
                BearAnimationBuilder.Attach(visual,motor,Output);
                motor.diagnosticLabel="マスターくま制作試作：全身短毛・全身リグ・歩行／ジャンプ";

                string[] roomNames={"Oak","OakLight","Wall","Trim","BlueTextile","IvoryTextile","Rug","Curtain","BookBlue","BookCream","BookRose","Pages","Leaf","Pot","Gold","StarGlow","LampShade",
                    "Rattan","RattanShade","KnitIce","KnitShade","RugBlue","DistantPine"};
                Color[] colors={new Color(.58f,.40f,.23f),new Color(.65f,.48f,.30f),new Color(.63f,.55f,.48f),new Color(.78f,.70f,.59f),
                    new Color(.29f,.43f,.60f),new Color(.81f,.77f,.66f),new Color(.74f,.70f,.62f),new Color(.64f,.53f,.49f),
                    new Color(.21f,.34f,.49f),new Color(.77f,.67f,.47f),new Color(.56f,.32f,.34f),new Color(.82f,.78f,.65f),
                    new Color(.18f,.35f,.14f),new Color(.53f,.37f,.24f),new Color(.85f,.61f,.23f),new Color(1,.72f,.26f),new Color(.89f,.72f,.46f),
                    new Color(.64f,.48f,.29f),new Color(.51f,.36f,.21f),new Color(.69f,.74f,.79f),new Color(.62f,.68f,.75f),new Color(.55f,.63f,.73f),new Color(.22f,.26f,.34f)};
                var roomMaterials=new Material[roomNames.Length];
                for(int i=0;i<roomNames.Length;i++)
                {
                    bool cloth=(i>=4 && i<=7) || (i>=19 && i<=21);
                    roomMaterials[i]=Mat(roomNames[i],colors[i],i<2?.42f:(i==14?.6f:.08f),cloth?normal:null,cloth);
                    if(i==15 || i==16)
                    { roomMaterials[i].SetColor("_EmissiveColor",i==15?new Color(480,210,48):new Color(14,7,2)); HDMaterial.ValidateMaterial(roomMaterials[i]); }
                }
                var wood=WoodTexture(); foreach(int i in new[]{0,1}) roomMaterials[i].SetTexture("_BaseColorMap",wood);
                TwilightSurfaceBuilder.Apply(roomMaterials,Output);
                ImportModel(Output+"/Room.fbx",roomMaterials,false);
                var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Room.fbx")); room.name="Editable_Twilight_Room";
                foreach(var renderer in room.GetComponentsInChildren<Renderer>())
                    if(renderer.sharedMaterials.Any(m=>m.name=="LampShade"))renderer.shadowCastingMode=ShadowCastingMode.Off;
                var forward=room.GetComponentsInChildren<Transform>().Single(t=>t.name=="RoomForward_UnityCheck");
                var right=room.GetComponentsInChildren<Transform>().Single(t=>t.name=="RoomRight_UnityCheck");
                if(forward.position.z<0) room.transform.rotation=Quaternion.Euler(0,180,0);
                if(right.position.x<0) room.transform.localScale=new Vector3(-1,1,1);
                if(forward.position.z<.99f || right.position.x<.99f) throw new Exception("Room coordinate markers failed.");
                var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.name="Room_Ceiling";ceiling.transform.position=new Vector3(0,2.65f,.25f);ceiling.transform.localScale=new Vector3(5,.10f,4.5f);ceiling.GetComponent<Renderer>().sharedMaterial=roomMaterials[2];
                Box("Floor_Collision",new Vector3(0,-.06f,.2f),new Vector3(5,.1f,4.6f));
                Box("Bed_Collision",new Vector3(-1.45f,.27f,.7f),new Vector3(1.52f,.54f,2));
                Box("Shelf_Collision",new Vector3(1.62f,.5f,.77f),new Vector3(.88f,1,.40f));
                Box("Cabinet_Collision",new Vector3(-.63f,.28f,1.38f),new Vector3(.43f,.56f,.4f));
                Box("Basket_Collision",new Vector3(1.02f,.15f,.20f),new Vector3(.44f,.29f,.32f));
                Box("Rug_Collision",new Vector3(-.23f,.006f,-.57f),new Vector3(1.32f,.036f,.80f));
                Box("Boundary_Left",new Vector3(-2.5f,.6f,.2f),new Vector3(.1f,1.2f,4.6f));
                Box("Boundary_Right",new Vector3(2.5f,.6f,.2f),new Vector3(.1f,1.2f,4.6f));
                Box("Boundary_Back",new Vector3(0,.6f,2.3f),new Vector3(5,1.2f,.1f));
                Box("Boundary_Front",new Vector3(0,.6f,-2.05f),new Vector3(5,1.2f,.1f));

                var game=Cam("GameplayCamera",new Vector3(.20f,.34f,-1.65f),new Vector3(-.12f,.28f,-.50f),48);
                game.enabled=true; game.tag="MainCamera"; var orbit=game.gameObject.AddComponent<OrbitCamera>(); orbit.target=motor; orbit.pitch=3; orbit.yaw=-16; orbit.distance=1.2f; motor.view=game.transform;
                Cam("S01_Portrait",new Vector3(-.12f,.30f,-1.08f),new Vector3(-.12f,.285f,-.5f),38);
                Cam("S02_Gameplay",new Vector3(.68f,.28f,-1.75f),new Vector3(0,.43f,.45f),48);
                Cam("S03_ThreeQuarter",new Vector3(.21f,.28f,-1.12f),new Vector3(-.12f,.21f,-.5f),38);
                Cam("S04_Contact",new Vector3(.18f,.10f,-.83f),new Vector3(-.12f,.035f,-.5f),36);

                Cam("S05_Bedding",new Vector3(-.25f,1.05f,-.65f),new Vector3(-1.45f,.52f,.65f),43);
                Cam("S06_Back",new Vector3(-.35f,.28f,.05f),new Vector3(-.12f,.23f,-.5f),38);

                var moon=MakeLight("Twilight_Window_Directional",LightType.Directional,new Vector3(0,2,2),LightUnit.Lux,16,new Color(.72f,.80f,1));
                moon.transform.rotation=Quaternion.LookRotation(new Vector3(-.3f,-.75f,-1)); moon.GetComponent<HDAdditionalLightData>().angularDiameter=6;
                var window=MakeLight("Window_Area_FILL_PREVIEW",LightType.Rectangle,new Vector3(.35f,1.35f,2.18f),LightUnit.Lumen,1050,new Color(.65f,.76f,1));
                window.areaSize=new Vector2(1.5f,1.4f); window.transform.LookAt(new Vector3(-.12f,.2f,-.5f)); window.shadows=LightShadows.None;
                var lamp=MakeLight("Bedside_Lamp",LightType.Point,new Vector3(-.63f,.91f,1.38f),LightUnit.Lumen,240,new Color(1,.62f,.32f)); lamp.range=2.8f; lamp.shapeRadius=.085f; lamp.GetComponent<HDAdditionalLightData>().softnessScale=2;
                var fill=MakeLight("Warm_Portrait_FILL_PREVIEW",LightType.Spot,new Vector3(.3f,.70f,-1.0f),LightUnit.Lumen,60,new Color(1,.88f,.72f));
                fill.shadows=LightShadows.None; fill.spotAngle=95; fill.range=3; fill.transform.LookAt(new Vector3(-.12f,.2f,-.5f));
                var star=MakeLight("Near_Star_Gold_Bounce",LightType.Point,new Vector3(.34f,.16f,-.10f),LightUnit.Lumen,35,new Color(1,.48f,.12f)); star.range=.7f; star.shadows=LightShadows.None;
                var backStar=MakeLight("Distant_Star_Gold_Bounce",LightType.Point,new Vector3(1.52f,1.12f,.70f),LightUnit.Lumen,25,new Color(1,.48f,.12f)); backStar.range=.7f; backStar.shadows=LightShadows.None;
                var soft=MakeLight("Portrait_Softbox_FILL_PREVIEW",LightType.Rectangle,new Vector3(.3f,.75f,-1.2f),LightUnit.Lumen,330,new Color(1,.88f,.72f));
                soft.areaSize=new Vector2(.8f,.8f); soft.transform.LookAt(new Vector3(-.12f,.25f,-.5f)); soft.shadows=LightShadows.None;
                var bounce=MakeLight("Warm_Wall_Bounce_ART_FILL",LightType.Rectangle,new Vector3(-1.9f,1.25f,1.3f),LightUnit.Lumen,290,new Color(1,.70f,.45f));
                bounce.areaSize=new Vector2(1.2f,1.4f);bounce.transform.LookAt(new Vector3(.4f,.45f,.20f));bounce.shadows=LightShadows.None;
                MakeVolume();
                var probeObject=new GameObject("Room_Reflection_OnceOnEnable");probeObject.transform.position=new Vector3(0,.85f,.3f);
                var probe=probeObject.AddComponent<ReflectionProbe>();probe.size=new Vector3(5,3,5);probe.boxProjection=true;probe.resolution=128;probe.cullingMask=~(1<<2);
                var hdProbe=probeObject.AddComponent<HDAdditionalReflectionData>();hdProbe.mode=ProbeSettings.Mode.Realtime;hdProbe.realtimeMode=ProbeSettings.RealtimeMode.OnEnable;
                hdProbe.influenceVolume.boxSize=new Vector3(5,3,5);hdProbe.RequestRenderNextUpdate();
                AssetDatabase.SaveAssets();
                if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Scene save failed; partial outputs preserved.");
                AssetDatabase.Refresh(); GeneratedContentGuard.WriteReceipt(folder,Application.unityVersion,Hdrp,Recipe); AssetDatabase.Refresh();
                Debug.Log("SORAJIRO TWILIGHT ASSETS: master bear height="+bounds.size.y+"m; full-body pile skin; room axes "+right.position+" / "+forward.position+"; "+ScenePath+". Quality, animation and performance unapproved.");
            }
            finally {if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);}
        }

        [MenuItem("Sorajiro/Twilight/3. Open twilight lookdev")]
        public static void Open() {Preflight(); if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new Exception("Create Twilight first."); EditorSceneManager.OpenScene(ScenePath);}

        private static void ImportModel(string path,Material[] materials,bool skin)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=skin?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
            importer.importAnimation=false; importer.optimizeGameObjects=false;
            if(skin) {importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; importer.motionNodeName="root";} importer.importNormals=ModelImporterNormals.Import;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var material in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);
            importer.SaveAndReimport();
        }

        private static Material Mat(string name,Color srgb,float smooth,Texture2D normal=null,bool doubleSided=false)
        {
            var m=new Material(Shader.Find("HDRP/Lit")){name=name}; m.SetColor("_BaseColor",srgb); m.SetFloat("_Smoothness",smooth);
            if(normal){m.SetTexture("_NormalMap",normal); m.SetTextureScale("_BaseColorMap",new Vector2(8,8)); m.SetFloat("_NormalScale",.16f);}
            if(doubleSided){m.SetFloat("_DoubleSidedEnable",1); m.SetFloat("_DoubleSidedNormalMode",name.StartsWith("Pile") || name.StartsWith("Rug")?2:0);}
            if(name.StartsWith("Pile") || name.StartsWith("Knit") || name.StartsWith("Rug"))
            {m.SetFloat("_MaterialID",4); m.SetColor("_SpecularColor",new Color(.008f,.008f,.008f));}
            HDMaterial.ValidateMaterial(m); AssetDatabase.CreateAsset(m,Output+"/"+name+".mat"); return m;
        }

        private static Texture2D WoodTexture()
        {
            var t=new Texture2D(256,512,TextureFormat.RGB24,false);
            for(int y=0;y<512;y++) for(int x=0;x<256;x++)
            {float n=Mathf.PerlinNoise(x*.09f+Mathf.Sin(y*.008f)*.12f,y*.001f); t.SetPixel(x,y,Color.Lerp(new Color(.83f,.79f,.74f),new Color(.96f,.94f,.91f),n));}
            t.Apply(); string p=Output+"/OakGrain_original.png"; File.WriteAllBytes(p,t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport); return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        private static void Box(string name,Vector3 pos,Vector3 size) {var obj=new GameObject(name); obj.transform.position=pos; obj.AddComponent<BoxCollider>().size=size;}

        private static Camera Cam(string name,Vector3 pos,Vector3 target,float fov)
        {
            var o=new GameObject(name); o.transform.position=pos; o.transform.LookAt(target); var c=o.AddComponent<Camera>(); c.fieldOfView=fov; c.nearClipPlane=.015f; c.farClipPlane=30; c.enabled=false;
            var hd=o.AddComponent<HDAdditionalCameraData>(); hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; hd.allowDynamicResolution=false; hd.allowDeepLearningSuperSampling=false; hd.allowFidelityFX2SuperResolution=false;
            return c;
        }

        private static Light MakeLight(string name,LightType type,Vector3 pos,LightUnit unit,float value,Color color)
        {
            var o=new GameObject(name); o.transform.position=pos; var l=o.AddComponent<Light>(); l.type=type; var hd=o.AddComponent<HDAdditionalLightData>();
            l.lightUnit=unit; l.intensity=value; l.color=color; l.shadows=LightShadows.Soft; l.shadowBias=.02f; l.shadowNormalBias=.05f;
            hd.useContactShadow.useOverride=true; hd.useContactShadow.@override=true; hd.shadowResolution.useOverride=true; hd.shadowResolution.@override=2048;
            return l;
        }

        private static void MakeVolume()
        {
            var p=ScriptableObject.CreateInstance<VolumeProfile>(); var exp=p.Add<Exposure>(); exp.mode.Override(ExposureMode.Fixed); exp.fixedExposure.Override(6.5f);
            var sky=p.Add<GradientSky>(); sky.top.Override(new Color(.13f,.20f,.42f).linear); sky.middle.Override(new Color(.68f,.37f,.49f).linear); sky.bottom.Override(new Color(.82f,.40f,.30f).linear); sky.gradientDiffusion.Override(3);sky.exposure.Override(6.0f); sky.updateMode.Override(EnvironmentUpdateMode.OnChanged);
            var ve=p.Add<VisualEnvironment>(); ve.skyType.Override(SkySettings.GetUniqueID<GradientSky>()); ve.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            p.Add<VolumetricClouds>().enable.Override(false); p.Add<CloudLayer>().opacity.Override(0); p.Add<DepthOfField>().focusMode.Override(DepthOfFieldMode.Off); p.Add<MotionBlur>().intensity.Override(0);
            var bloom=p.Add<Bloom>(); bloom.intensity.Override(.25f); bloom.threshold.Override(.8f);
            var reflections=p.Add<ScreenSpaceReflection>();reflections.enabled.Override(true);reflections.tracing.Override(RayCastingMode.RayMarching);
            reflections.minSmoothness=.35f;reflections.smoothnessFadeStart=.52f;reflections.reflectSky.Override(false);
            foreach(var parameter in reflections.parameters)parameter.overrideState=true;
            p.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var ao=p.Add<ScreenSpaceAmbientOcclusion>(); ao.intensity.Override(.8f); ao.radius.Override(.12f);
            var contact=p.Add<ContactShadows>(); contact.enable.Override(true); contact.length.Override(.12f); contact.maxDistance.Override(5); contact.distanceScaleFactor.Override(0);
            p.Add<HDShadowSettings>().maxShadowDistance.Override(8);
            p.Add<IndirectLightingController>().indirectDiffuseLightingMultiplier.Override(1.2f);
            AssetDatabase.CreateAsset(p,Output+"/LookDevVolume.asset"); foreach(var component in p.components) AssetDatabase.AddObjectToAsset(component,p); EditorUtility.SetDirty(p);
            var volume=new GameObject("Twilight_FixedEV6_5_NO_DOF_NO_BLUR").AddComponent<Volume>(); volume.isGlobal=true; volume.priority=100; volume.sharedProfile=p;
        }
    }
}
