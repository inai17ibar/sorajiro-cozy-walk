using System;
using System.Collections.Generic;
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
    // A protected branch of the v010 scene. Original scenes, imported meshes and materials are read-only inputs.
    public static class BeddingRefinementBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/Bedside_v004";
        public const string ScenePath=Output+"/L_LookDev.unity";
        public const string Source="Assets/Sorajiro/Generated/Twilight_v010";
        public const string FabricPath="Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/ShaderGraph/Fabric CottonWool.shadergraph";
        private static string RoomSource=>Path.Combine(Repo,"Artifacts/SourceArt/Room/twilight_v008");
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Preparation=>Path.Combine(Repo,"Artifacts/UnityEditor/Bedside_v004_prebake.json");
        private static string Recipe=>"bedside-v004-"+GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/BeddingRefinementBuilder.cs"))+
            GeneratedContentGuard.HashFile(Path.Combine(RoomSource,"receipt.json"))+GeneratedContentGuard.HashFile(Source+"/generation-receipt.json")+GeneratedContentGuard.HashFile(FabricFullPath);
        private static string FabricFullPath=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).resolvedPath+"/Runtime/RenderPipelineResources/ShaderGraph/Fabric CottonWool.shadergraph";

        private static void RequireIdle()
        {
            if(Application.unityVersion!="6000.6.5f1" || Hdrp!="17.6.0" || !(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))throw new Exception("Reviewed Unity / HDRP required");
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Lightmapping.isRunning)throw new Exception("Stop Play, compilation and other bakes first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve and save owner edits before generation");
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked output refused");
            VerifySource();
            VerifyRoom();
        }
        private static void VerifySource()
        {
            var receipt=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(Source+"/generation-receipt.json"));
            GeneratedContentGuard.CanReuse(Path.GetFullPath(Source),Application.unityVersion,Hdrp,receipt.recipe);
        }

        private static void VerifyRoom()
        {
            string receipt=File.ReadAllText(Path.Combine(RoomSource,"receipt.json"));
            foreach(string name in new[]{"TwilightRoom.blend","TwilightRoom.fbx"})
            {
                string path=Path.Combine(RoomSource,name);
                for(var d=new DirectoryInfo(Path.GetDirectoryName(path));d!=null;d=d.Parent)
                    if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked source refused");
                if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0 || !receipt.Contains("\""+name+"\": \""+GeneratedContentGuard.HashFile(path)+"\""))throw new Exception("Edited bedding source; preserve and use another revision");
            }
        }
        private static void ReplaceRoom()
        {
            File.Copy(Path.Combine(RoomSource,"TwilightRoom.fbx"),Output+"/Room.fbx",false);
            AssetDatabase.ImportAsset(Output+"/Room.fbx",ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Output+"/Room.fbx");
            importer.animationType=ModelImporterAnimationType.None;importer.importAnimation=false;
            importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Source}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),m);
            }
            importer.SaveAndReimport();
            var original=GameObject.Find("Editable_Twilight_Room");if(!original)throw new Exception("Original room not found");
            var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Room.fbx"));
            replacement.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);replacement.transform.localScale=original.transform.localScale;
            UnityEngine.Object.DestroyImmediate(original);replacement.name="Editable_Twilight_Room";
            foreach(var r in replacement.GetComponentsInChildren<Renderer>())
                if(r.sharedMaterials.Any(m=>m && m.name=="LampShade"))r.shadowCastingMode=ShadowCastingMode.Off;
            foreach(var marker in new[]{("RoomForward_UnityCheck",Vector3.forward),("RoomRight_UnityCheck",Vector3.right)})
            {
                var t=replacement.GetComponentsInChildren<Transform>().Single(x=>x.name==marker.Item1);
                if(Vector3.Dot(t.position,marker.Item2)<.99f)throw new Exception("Bedding room axes mismatch");
            }
            // Physical subfloor closes board gaps; a narrow wall-junction backing closes the room corner.
            // Both are ordinary closed scene meshes retained in the render/bake, outside the visible room surface.
            Seal("Bedding_Subfloor_Seal",new Vector3(0,-.085f,.25f),new Vector3(5.1f,.08f,4.6f));
            Seal("Bedding_Wall_Junction_Seal",new Vector3(-2.50f,1.30f,2.30f),new Vector3(.18f,2.60f,.18f));
        }
        private static void Seal(string name,Vector3 position,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.position=position;obj.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Source+"/Wall.mat");
        }

        [MenuItem("Sorajiro/Bedding/1. Create protected fabric and GI trial (Edit)")]
        public static void Create()
        {
            RequireIdle();
            if(GeneratedContentGuard.CanReuse(Path.GetFullPath(Output),Application.unityVersion,Hdrp,Recipe))return;
            if(File.Exists(Preparation))throw new Exception("Existing preparation record preserved; use a new revision");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            if(!AssetDatabase.CopyAsset(Source+"/L_LookDev.unity",ScenePath))throw new Exception("Scene copy failed");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            ReplaceRoom();
            var originals=new Dictionary<string,Material>();
            var fabrics=new Dictionary<string,Material>();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(FabricPath);
            if(!shader)throw new Exception("Installed HDRP Cotton/Wool Shader Graph not found");
            var thread=Texture("OriginalCottonThread",(u,v)=>new Color(.96f,.5f+.055f*Mathf.Sin(u*Mathf.PI*96),.10f,.5f+.055f*Mathf.Sin(v*Mathf.PI*96)));
            var fuzz=Texture("OriginalCottonFuzz",(u,v)=>Color.white*(.72f+.20f*Mathf.PerlinNoise(u*118+12,v*118+37)));
            foreach(string name in new[]{"BlueTextile","IvoryTextile","Curtain","KnitIce","KnitShade"})
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>(Source+"/"+name+".mat");originals.Add(name,source);
                var m=new Material(shader){name=name+"_CottonWool"};
                m.SetColor("_BaseColor",source.GetColor("_BaseColor"));
                m.SetTexture("_BaseColorMap",source.GetTexture("_BaseColorMap") ?? Texture2D.whiteTexture);
                m.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"/WovenCotton_Normal.png"));
                m.SetFloat("_NormalMapStrength",.18f);m.SetFloat("_SmoothnessMin",.02f);m.SetFloat("_SmoothnessMax",.12f);
                m.SetColor("_SpecularColor",new Color(.04f,.04f,.04f));
                m.SetFloat("_SubsurfaceMask",0);m.SetFloat("_TransmissionEnable",0);
                m.SetFloat("_DoubleSidedEnable",1);m.SetFloat("_DoubleSidedNormalMode",0);
                m.SetTexture("_ThreadMap",thread);m.SetFloat("_useThreadMap",1);m.SetFloat("_ThreadNormalStrength",.35f);
                m.SetFloat("_ThreadAOStrength01",.15f);m.SetFloat("_ThreadSmoothnessScale",.08f);
                m.SetTexture("_FuzzMap",fuzz);m.SetFloat("_FuzzStrength",.12f);m.SetFloat("_FuzzMapUVScale",4);
                m.SetVector("_uvBaseST",name=="BlueTextile"?new Vector4(2,3,0,0):new Vector4(1,1,0,0));
                m.SetVector("_uvThreadST",new Vector4(12,12,0,0));
                m.EnableKeyword("_BASE_UV_CHANNEL_UV0");m.EnableKeyword("_THREADS_UV_CHANNEL_UV0");
                HDMaterial.ValidateMaterial(m);AssetDatabase.CreateAsset(m,Output+"/"+m.name+".mat");fabrics.Add(name,m);
            }
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m && fabrics.TryGetValue(m.name,out var fabric)?fabric:m).ToArray();
                renderer.lightProbeUsage=LightProbeUsage.BlendProbes;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            // Only large static surfaces enter lightmaps; dense loop/knit/fur meshes stay probe-lit.
            int unwrapped=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                if(!new[]{"Room_Oak","Room_OakLight","Room_Wall","Room_Trim","Room_BlueTextile","Room_IvoryTextile","Room_Curtain","Room_LampShade","Room_Ceiling","Bedding_Subfloor_Seal","Bedding_Wall_Junction_Seal"}.Contains(filter.name))continue;
                var copy=UnityEngine.Object.Instantiate(filter.sharedMesh);copy.name=filter.name+"_LightmapUV";
                UnwrapParam.SetDefaults(out var unwrap);unwrap.packMargin=.006f;
                if(!Unwrapping.GenerateSecondaryUVSet(copy,unwrap))throw new Exception("UV2 unwrap failed: "+filter.name);
                AssetDatabase.CreateAsset(copy,Output+"/"+copy.name+".asset");filter.sharedMesh=copy;
                var renderer=filter.GetComponent<MeshRenderer>();renderer.receiveGI=ReceiveGI.Lightmaps;renderer.scaleInLightmap=1;
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject,StaticEditorFlags.ContributeGI);unwrapped++;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter.gameObject);
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Source+"/LookDevVolume.asset");
            var newProfile=UnityEngine.Object.Instantiate(profile);newProfile.name="Bedside_BakedSky_Volume";newProfile.components.Clear();
            AssetDatabase.CreateAsset(newProfile,Output+"/LookDevVolume.asset");
            foreach(var component in profile.components)
            {var clone=UnityEngine.Object.Instantiate(component);clone.name=component.name;newProfile.components.Add(clone);AssetDatabase.AddObjectToAsset(clone,newProfile);}
            EditorUtility.SetDirty(newProfile);
            UnityEngine.Object.FindObjectsByType<Volume>().Single(v=>v.sharedProfile==profile).sharedProfile=newProfile;
            if(newProfile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao)){ao.intensity.Override(.55f);ao.radius.Override(.10f);}
            if(newProfile.TryGet<Exposure>(out var exposure))exposure.fixedExposure.Override(6.0f);
            if(newProfile.TryGet<IndirectLightingController>(out var indirect))indirect.indirectDiffuseLightingMultiplier.Override(1.6f);
            var staticSky=new GameObject("Baked_Twilight_Sky").AddComponent<StaticLightingSky>();staticSky.profile=newProfile;staticSky.staticLightingSkyUniqueID=SkySettings.GetUniqueID<GradientSky>();
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                light.lightmapBakeType=LightmapBakeType.Mixed;light.bounceIntensity=1;
                if(light.name.Contains("FILL_PREVIEW") || light.name=="Warm_Wall_Bounce_ART_FILL")light.gameObject.SetActive(false);
                if(light.name=="Twilight_Window_Directional"){light.intensity=8;light.GetComponent<HDAdditionalLightData>().angularDiameter=9;}
                if(light.name=="Window_Area_FILL_PREVIEW")
                {light.gameObject.SetActive(true);light.lightmapBakeType=LightmapBakeType.Baked;light.intensity=1100;light.shadows=LightShadows.Soft;}
                if(light.name=="Bedside_Lamp")
                {light.lightmapBakeType=LightmapBakeType.Baked;light.intensity=240;light.color=new Color(1,.71f,.46f);light.shadowNormalBias=.015f;light.shapeRadius=.12f;}
                // Art-directed broad key keeps the hero readable while the room receives actual baked lighting.
                if(light.name=="Portrait_Softbox_FILL_PREVIEW")
                {light.gameObject.SetActive(true);light.name="Hero_Soft_Key_ART_FILL";light.lightmapBakeType=LightmapBakeType.Realtime;light.intensity=300;light.color=new Color(1,.92f,.82f);}
            }
            var lamp=new GameObject("Lamp_Shade_Baked_Soft_Area").AddComponent<Light>();lamp.gameObject.AddComponent<HDAdditionalLightData>();
            lamp.type=LightType.Rectangle;lamp.lightUnit=LightUnit.Lumen;lamp.intensity=160;lamp.color=new Color(1,.72f,.48f);
            lamp.lightmapBakeType=LightmapBakeType.Baked;lamp.shadows=LightShadows.Soft;lamp.areaSize=new Vector2(.28f,.24f);
            lamp.transform.position=new Vector3(-.63f,.92f,1.34f);lamp.transform.LookAt(new Vector3(-1.30f,.50f,.50f));
            if(((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).currentPlatformRenderPipelineSettings.lightProbeSystem!=RenderPipelineSettings.LightProbeSystem.AdaptiveProbeVolumes)
                throw new Exception("This trial requires the project's existing APV support; owner pipeline asset is not modified");
            var apv=new GameObject("Bedside_Adaptive_Probe_Volume").AddComponent<ProbeVolume>();apv.mode=ProbeVolume.Mode.Local;
            apv.transform.position=new Vector3(0,1.25f,.15f);apv.size=new Vector3(4.8f,2.5f,4.3f);
            var set=ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();set.name="Bedside_Local_APV_BakingSet";
            set.minDistanceBetweenProbes=.25f;set.simplificationLevels=2;set.minRendererVolumeSize=.02f;set.skyOcclusion=false;
            AssetDatabase.CreateAsset(set,Output+"/APV_BakingSet.asset");
            if(!set.TryAddScene(AssetDatabase.AssetPathToGUID(ScenePath)))throw new Exception("New scene unexpectedly belongs to another baking set");
            var adjust=new GameObject("Bedding_APV_Validity").AddComponent<ProbeAdjustmentVolume>();
            adjust.mode=ProbeAdjustmentVolume.Mode.OverrideValidityThreshold;adjust.overriddenDilationThreshold=.25f;
            adjust.transform.position=new Vector3(-1.45f,.65f,.75f);adjust.size=new Vector3(1.65f,1.25f,2.30f);
            var data=new GameObject("Bedside_APV_SceneData").AddComponent<ProbeVolumePerSceneData>();
            var serialized=new SerializedObject(data);serialized.FindProperty("serializedBakingSet").objectReferenceValue=set;serialized.ApplyModifiedPropertiesWithoutUndo();
            ProbeReferenceVolume.instance.SetActiveBakingSet(set);EditorUtility.SetDirty(set);
            var settings=new LightingSettings{name="Bedside_GPU_GI"};settings.bakedGI=true;settings.realtimeGI=false;
            settings.lightmapper=LightingSettings.Lightmapper.ProgressiveGPU;settings.mixedBakeMode=MixedLightingMode.IndirectOnly;
            settings.lightmapResolution=64;settings.lightmapMaxSize=1024;settings.directSampleCount=64;settings.indirectSampleCount=128;
            settings.environmentSampleCount=64;settings.maxBounces=3;settings.ao=false;
            AssetDatabase.CreateAsset(settings,Output+"/LightingSettings.asset");Lightmapping.lightingSettings=settings;
            LightmapSettings.lightmapsMode=LightmapsMode.CombinedDirectional;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.Refresh();
            var record=new PreparationRecord{recipe=Recipe,scene=ScenePath,unwrappedMeshes=unwrapped,probes=0,
                files=GeneratedContentGuard.Files(Output).Select(p=>Path.GetRelativePath(Output,p)).ToArray(),
                hashes=GeneratedContentGuard.Files(Output).Select(GeneratedContentGuard.HashFile).ToArray()};
            if(File.Exists(Preparation))throw new Exception("Existing preparation record preserved; create a new revision");
            Directory.CreateDirectory(Path.GetDirectoryName(Preparation));File.WriteAllText(Preparation,JsonUtility.ToJson(record,true));
            Debug.Log("SORAJIRO BEDSIDE PREPARED: "+unwrapped+" static UV2 meshes, bounded APV with 0.25m minimum spacing, Cotton/Wool fabrics. Bake pending; no final receipt.");
        }

        [MenuItem("Sorajiro/Bedding/2. Bake new corner (GPU, Edit)")]
        public static void BakeMenu(){Bake();}
        public static string Bake()
        {
            RequireIdle();if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open prepared corner");
            if(File.Exists(Output+"/generation-receipt.json"))throw new Exception("Completed bake preserved; use a new revision to rebake");
            var preparation=JsonUtility.FromJson<PreparationRecord>(File.ReadAllText(Preparation));
            var files=GeneratedContentGuard.Files(Output);
            if(preparation.recipe!=Recipe || !files.Select(p=>Path.GetRelativePath(Output,p)).SequenceEqual(preparation.files) || !files.Select(GeneratedContentGuard.HashFile).SequenceEqual(preparation.hashes))throw new Exception("Prepared scene edited; preserve and use new revision");
            bakePath=Path.Combine(Repo,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_bedding-bake.json");
            Directory.CreateDirectory(Path.GetDirectoryName(bakePath));bake=new BakeRecord{state="baking",scene=ScenePath,startedUtc=DateTime.UtcNow.ToString("O"),gpu=SystemInfo.graphicsDeviceName};
            SaveBake();started=EditorApplication.timeSinceStartup;Lightmapping.bakeCompleted+=OnBakeCompleted;EditorApplication.update+=Monitor;
            try{if(!Lightmapping.BakeAsync())throw new Exception("GPU bake failed to start");}
            catch(Exception e){Fail(e);throw;}return bakePath;
        }
        private static string bakePath;private static BakeRecord bake;private static double started;private static bool finalizing;
        private static void OnBakeCompleted(){finalizing=true;}
        private static void Monitor()
        {
            try
            {
                if(SceneManager.GetActiveScene().path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Bake interrupted by scene/play change");
                if(EditorApplication.timeSinceStartup-started>900){Lightmapping.Cancel();throw new Exception("Bake exceeded 15-minute trial limit; partial output preserved");}
                if(!finalizing && !Lightmapping.isRunning && EditorApplication.timeSinceStartup-started>30)throw new Exception("GPU bake stopped without a completion event; partial outputs preserved");
                if(!finalizing)return;
                if(Lightmapping.isRunning)return;
                var maps=LightmapSettings.lightmaps;var set=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Output+"/APV_BakingSet.asset");
                if(maps.Length==0 || maps.Any(m=>!m.lightmapColor) || !set || !set.HasBakedData() || !Lightmapping.lightingDataAsset)throw new Exception("Bake returned without real lightmaps/APV/data");
                VerifySource();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.Refresh();
                GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();
                bake.state="completed";bake.lightmaps=maps.Length;bake.probes=0;bake.apvBaked=true;bake.apvMinimumSpacing=.25f;bake.elapsedSeconds=EditorApplication.timeSinceStartup-started;
                bake.completedUtc=DateTime.UtcNow.ToString("O");SaveBake();Cleanup();Debug.Log("SORAJIRO BEDSIDE BAKE COMPLETE: "+bakePath);
            }
            catch(Exception e){Fail(e);}
        }
        private static void Fail(Exception e){bake.state="failed";bake.error=e.ToString();SaveBake();Cleanup();Debug.LogException(e);}
        private static void SaveBake(){File.WriteAllText(bakePath,JsonUtility.ToJson(bake,true));}
        private static void Cleanup(){Lightmapping.bakeCompleted-=OnBakeCompleted;EditorApplication.update-=Monitor;finalizing=false;}
        public static void Check()
        {
            RequireIdle();if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open baked corner");
            if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Bake not completed");
            int maps=LightmapSettings.lightmaps.Length;if(maps==0 || !Lightmapping.lightingDataAsset)throw new Exception("No persisted baked maps");
            var set=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Output+"/APV_BakingSet.asset");
            if(!set || !set.HasBakedData() || !set.sceneGUIDs.Contains(AssetDatabase.AssetPathToGUID(ScenePath)))throw new Exception("APV baked data or scene membership missing");
            foreach(string name in new[]{"BlueTextile","IvoryTextile","Curtain","KnitIce","KnitShade"})
            {var mat=AssetDatabase.LoadAssetAtPath<Material>(Output+"/"+name+"_CottonWool.mat");if(!mat || mat.shader!=AssetDatabase.LoadAssetAtPath<Shader>(FabricPath) || !mat.GetTexture("_ThreadMap"))throw new Exception("Fabric binding lost "+name);}
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Room.fbx");
            if(!model || GeneratedContentGuard.HashFile(Output+"/Room.fbx")!=GeneratedContentGuard.HashFile(Path.Combine(RoomSource,"TwilightRoom.fbx")))throw new Exception("New room import differs from protected source");
            if(!GameObject.Find("Bedding_Subfloor_Seal") || !GameObject.Find("Bedding_Wall_Junction_Seal") || !UnityEngine.Object.FindAnyObjectByType<ProbeAdjustmentVolume>())throw new Exception("Bedding leak-control geometry/volume missing");
            string result="PASS: persisted GPU lightmaps="+maps+", actual APV HasBakedData=true; minimum probe spacing="+set.minDistanceBetweenProbes+"m; APV runtime data loaded="+ProbeReferenceVolume.instance.DataHasBeenLoaded()+"; 5 installed HDRP Cotton/Wool materials with original linear thread/fuzz maps.\nScene="+ScenePath+"\nActual baked data and bindings; not visual approval or performance.\n";
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/bedding-checks.txt"),result);Debug.Log("SORAJIRO BEDSIDE CHECK: "+result);
        }
        public static void Open(){RequireIdle();EditorSceneManager.OpenScene(ScenePath);}
        private static Texture2D Texture(string name,Func<float,float,Color> pixel)
        {
            const int size=512;var t=new Texture2D(size,size,TextureFormat.RGBA32,false,true);var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)colors[y*size+x]=pixel((float)x/size,(float)y/size);
            t.SetPixels(colors);t.Apply();string path=Output+"/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=false;ti.wrapMode=TextureWrapMode.Repeat;ti.anisoLevel=8;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        [Serializable]private class PreparationRecord{public string recipe,scene;public int unwrappedMeshes,probes;public string[] files,hashes;}
        [Serializable]private class BakeRecord{public string state,scene,startedUtc,completedUtc,gpu,error;public int lightmaps,probes;public bool apvBaked;public float apvMinimumSpacing;public double elapsedSeconds;}
    }
}
