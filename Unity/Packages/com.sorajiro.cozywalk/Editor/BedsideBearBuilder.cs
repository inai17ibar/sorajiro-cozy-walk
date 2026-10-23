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
    // Preserve the owner's selected v001/3 as an exact model/material/controller input.
    public static class BedsideBearBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/BedsideBear_v002";
        public const string ScenePath=Output+"/L_LookDev.unity";
        public const string Chosen="Assets/Sorajiro/Generated/BearFit_v001";
        public const string Room=BeddingStableBuilder.Output;
        public const string ModelName="Preferred_BearFit_v001_3";
        public const string LightsName="Preferred_BearFit_v001_3_Lights";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Preparation=>Path.Combine(Repo,"Artifacts/UnityEditor/"+Path.GetFileName(Output)+"-preparation.json");
        private static string Recipe=>"bedside-bear-v002-"+GeneratedContentGuard.HashFile(Chosen+"/generation-receipt.json")+
            GeneratedContentGuard.HashFile(Room+"/generation-receipt.json")+GeneratedContentGuard.HashFile(FleeceStudyBuilder.Output+"/generation-receipt.json")+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/BedsideBearBuilder.cs"));
        public static void VerifyInputs()
        {
            foreach(string folder in new[]{Chosen,Room,FleeceStudyBuilder.Output})
            {
                for(var d=new DirectoryInfo(Path.GetFullPath(folder));d!=null;d=d.Parent)
                    if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked input refused");
                var r=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(folder+"/generation-receipt.json"));
                if(!GeneratedContentGuard.CanReuse(folder,Application.unityVersion,Hdrp,r.recipe))throw new Exception("Input missing/edited");
            }
        }
        public static void Preflight()
        {
            if(Application.unityVersion!="6000.6.5f1" || Hdrp!="17.6.0")throw new Exception("Reviewed Unity/HDRP required");
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Lightmapping.isRunning)throw new Exception("Stop Play/bake/compilation first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve owner scene edits first");
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked output refused");
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(!asset || !asset.currentPlatformRenderPipelineSettings.supportLightLayers)throw new Exception("Existing HDRP Light Layers required; pipeline will not be changed");
            VerifyInputs();
        }
        [MenuItem("Sorajiro/Bedside Bear/1. Create selected v001 bear in room")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            if(File.Exists(Preparation))throw new Exception("Partial preparation preserved; use another revision");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var source=EditorSceneManager.OpenScene(Room+"/L_LookDev.unity");
            // Clone authored roots, excluding the old per-scene APV record. Never register a new GUID in the old baking set.
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            foreach(var root in source.GetRootGameObjects())
            {
                if(root.GetComponent<ProbeVolumePerSceneData>() || root.GetComponent<ProbeVolume>())continue;
                var copy=UnityEngine.Object.Instantiate(root);copy.name=root.name;SceneManager.MoveGameObjectToScene(copy,scene);
            }
            EditorSceneManager.CloseScene(source,true);
            Lightmapping.lightingDataAsset=null;LightmapSettings.lightmaps=Array.Empty<LightmapData>();
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>()){r.lightmapIndex=-1;r.renderingLayerMask=(r.renderingLayerMask&~255u)|1u;}
            EditorSceneManager.SaveScene(scene,ScenePath);
            var roomMotor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();var old=roomMotor.visualRoot;
            var chosenScene=EditorSceneManager.OpenScene(Chosen+"/L_LookDev.unity",OpenSceneMode.Additive);
            var selection=chosenScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BearFitPreview>(true)).Single();
            // Do not call Apply or save the source scene. Clone precisely the third comparison's model and lights.
            var model=UnityEngine.Object.Instantiate(selection.currentModel);model.SetActive(true);model.name=ModelName;
            SceneManager.MoveGameObjectToScene(model,scene);model.transform.SetParent(roomMotor.transform,false);
            model.transform.localPosition=old.localPosition;model.transform.localRotation=old.localRotation;model.transform.localScale=selection.currentModel.transform.localScale;
            var lights=UnityEngine.Object.Instantiate(selection.currentLights);lights.SetActive(true);lights.name=LightsName;
            SceneManager.MoveGameObjectToScene(lights,scene);lights.transform.position=roomMotor.transform.position;
            // The selected exposure is inherited from the original studio's base profile (EV7).
            var studio=chosenScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Volume>(true)).Single(v=>v.name=="Fleece_Studio_Volume");
            if(!studio.sharedProfile.TryGet<Exposure>(out var chosenExposure))throw new Exception("Selected exposure unavailable");
            float ev=chosenExposure.fixedExposure.value;
            EditorSceneManager.CloseScene(chosenScene,true);SceneManager.SetActiveScene(scene);
            // Keep the room sky and its warm indirect lighting. Only hero direct lights use the selected studio settings.
            foreach(var volume in lights.GetComponentsInChildren<Volume>(true))UnityEngine.Object.DestroyImmediate(volume.gameObject);
            UnityEngine.Object.DestroyImmediate(old.gameObject);roomMotor.visualRoot=model.transform;
            roomMotor.GetComponent<BearAnimationDriver>().animator=model.GetComponent<Animator>();
            roomMotor.diagnosticLabel="BearFit v001の3を配置：WASD / Shift / Space";
            // Neutral broad fill compensates the different room ambient, without retinting the chosen materials.
            var adaptation=new GameObject("Room_Adaptation_Neutral_Fill").AddComponent<Light>();adaptation.gameObject.AddComponent<HDAdditionalLightData>();adaptation.transform.SetParent(lights.transform,false);
            adaptation.type=LightType.Rectangle;adaptation.lightUnit=LightUnit.Lumen;adaptation.intensity=240;adaptation.color=new Color(.94f,.97f,1);adaptation.areaSize=new Vector2(.70f,.60f);adaptation.range=5;adaptation.shadows=LightShadows.None;
            adaptation.transform.localPosition=new Vector3(0,.40f,-.48f);adaptation.transform.LookAt(roomMotor.transform.position+Vector3.up*.23f);
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {skin.renderingLayerMask=(skin.renderingLayerMask&~255u)|2u;skin.lightProbeUsage=LightProbeUsage.BlendProbes;skin.updateWhenOffscreen=true;}
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                bool hero=light.transform.IsChildOf(lights.transform);var hd=light.GetComponent<HDAdditionalLightData>();
                hd.SetLightLayer((UnityEngine.Rendering.HighDefinition.RenderingLayerMask)(hero?2u:1u),(UnityEngine.Rendering.HighDefinition.RenderingLayerMask)(hero?2u:3u));
                if(hero)light.lightmapBakeType=LightmapBakeType.Realtime;
                if(light.name=="Hero_Soft_Key_ART_FILL")light.gameObject.SetActive(false);
            }
            var roomVolume=UnityEngine.Object.FindObjectsByType<Volume>().Single(v=>v.sharedProfile && AssetDatabase.GetAssetPath(v.sharedProfile)==Room+"/LookDevVolume.asset");
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="PreferredBear_Bedside_Volume";AssetDatabase.CreateAsset(profile,Output+"/LookDevVolume.asset");
            foreach(var component in roomVolume.sharedProfile.components)
            {var clone=UnityEngine.Object.Instantiate(component);clone.name=component.name;profile.components.Add(clone);AssetDatabase.AddObjectToAsset(clone,profile);}
            profile.TryGet<Exposure>(out var exposure);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(ev);EditorUtility.SetDirty(profile);
            if(profile.TryGet<GlobalIllumination>(out var gi))gi.adaptiveProbeVolumesLayerMask.Override((UnityEngine.RenderingLayerMask)3u);
            roomVolume.sharedProfile=profile;foreach(var sky in UnityEngine.Object.FindObjectsByType<StaticLightingSky>())sky.profile=profile;
            var apv=new GameObject("SelectedBear_APV_Volume").AddComponent<ProbeVolume>();apv.mode=ProbeVolume.Mode.Local;apv.transform.position=new Vector3(0,1.25f,.15f);apv.size=new Vector3(4.8f,2.5f,4.3f);
            var set=ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();set.name="SelectedBear_Private_BakingSet";set.minDistanceBetweenProbes=.25f;set.simplificationLevels=2;set.minRendererVolumeSize=.02f;set.skyOcclusion=false;
            AssetDatabase.CreateAsset(set,Output+"/APV_BakingSet.asset");var authored=new SerializedObject(set);var scenarios=authored.FindProperty("m_LightingScenarios");scenarios.arraySize=1;scenarios.GetArrayElementAtIndex(0).stringValue="Default";authored.ApplyModifiedPropertiesWithoutUndo();
            if(!set.TryAddScene(AssetDatabase.AssetPathToGUID(ScenePath)))throw new Exception("Private scene membership failed");
            var data=new GameObject("SelectedBear_APV_SceneData").AddComponent<ProbeVolumePerSceneData>();var serialized=new SerializedObject(data);serialized.FindProperty("serializedBakingSet").objectReferenceValue=set;serialized.ApplyModifiedPropertiesWithoutUndo();ProbeReferenceVolume.instance.SetActiveBakingSet(set);
            var settings=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LightingSettings>(Room+"/LightingSettings.asset"));settings.name="SelectedBear_GPU_GI";AssetDatabase.CreateAsset(settings,Output+"/LightingSettings.asset");Lightmapping.lightingSettings=settings;LightmapSettings.lightmapsMode=LightmapsMode.CombinedDirectional;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);AssetDatabase.Refresh();VerifyInputs();
            var files=GeneratedContentGuard.Files(Output);var prep=new PreparationRecord{recipe=Recipe,files=files.Select(p=>Path.GetRelativePath(Output,p)).ToArray(),hashes=files.Select(GeneratedContentGuard.HashFile).ToArray()};
            File.WriteAllText(Preparation,JsonUtility.ToJson(prep,true));
        }
        private static string bakePath;private static BakeRecord bake;private static double started;private static bool finalizing;
        public static string Bake()
        {
            Preflight();if(SceneManager.GetActiveScene().path!=ScenePath || SceneManager.sceneCount!=1)throw new Exception("Only the private prepared scene may be open for bake");
            if(File.Exists(Output+"/generation-receipt.json"))throw new Exception("Final output preserved; use a new revision");
            var prep=JsonUtility.FromJson<PreparationRecord>(File.ReadAllText(Preparation));var files=GeneratedContentGuard.Files(Output);
            if(prep.recipe!=Recipe || !files.Select(p=>Path.GetRelativePath(Output,p)).SequenceEqual(prep.files)||!files.Select(GeneratedContentGuard.HashFile).SequenceEqual(prep.hashes))throw new Exception("Prepared data edited; preserve partial output");
            if(AssetDatabase.GetAssetPath(Lightmapping.lightingSettings)!=Output+"/LightingSettings.asset" || Lightmapping.lightingDataAsset)throw new Exception("Bake must own its new lighting settings and empty lighting data");
            bakePath=Path.Combine(Repo,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_bedbear-bake.json");Directory.CreateDirectory(Path.GetDirectoryName(bakePath));
            bake=new BakeRecord{state="baking",scene=ScenePath,startedUtc=DateTime.UtcNow.ToString("O"),gpu=SystemInfo.graphicsDeviceName};SaveBake();started=EditorApplication.timeSinceStartup;finalizing=false;
            Lightmapping.bakeCompleted+=Completed;EditorApplication.update+=Monitor;
            try{if(!Lightmapping.BakeAsync())throw new Exception("GPU bake did not start");}catch(Exception e){Fail(e);throw;}return bakePath;
        }
        private static void Completed(){finalizing=true;}
        private static void Monitor()
        {
            try
            {
                if(SceneManager.GetActiveScene().path!=ScenePath||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Bake scene changed");
                if(EditorApplication.timeSinceStartup-started>900){Lightmapping.Cancel();throw new Exception("15-minute bake limit; partial data preserved");}
                if(!finalizing && !Lightmapping.isRunning && EditorApplication.timeSinceStartup-started>30)throw new Exception("Bake stopped without completion");
                if(!finalizing||Lightmapping.isRunning)return;
                var set=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Output+"/APV_BakingSet.asset");var maps=LightmapSettings.lightmaps;
                if(maps.Length==0||maps.Any(m=>!m.lightmapColor)||!set.HasBakedData()||!Lightmapping.lightingDataAsset)throw new Exception("Bake has no real maps/APV");
                if(!AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset).StartsWith(Output+"/"))throw new Exception("Bake data not owned by new output");
                VerifyInputs();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.Refresh();AssetDatabase.ForceReserializeAssets(new[]{Output+"/APV_BakingSet.asset"});AssetDatabase.Refresh();
                GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();
                bake.state="completed";bake.completedUtc=DateTime.UtcNow.ToString("O");bake.elapsedSeconds=EditorApplication.timeSinceStartup-started;bake.lightmaps=maps.Length;bake.apvBaked=true;SaveBake();Cleanup();
            }
            catch(Exception e){Fail(e);}
        }
        private static void Fail(Exception e){bake.state="failed";bake.error=e.ToString();SaveBake();Cleanup();Debug.LogException(e);}
        private static void SaveBake(){File.WriteAllText(bakePath,JsonUtility.ToJson(bake,true));}
        private static void Cleanup(){Lightmapping.bakeCompleted-=Completed;EditorApplication.update-=Monitor;finalizing=false;}
        public static void Check()
        {
            Preflight();if(SceneManager.GetActiveScene().path!=ScenePath || !GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Open completed selected-bear scene");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();var model=motor.visualRoot;
            if(model.name!=ModelName || AssetDatabase.GetAssetPath(motor.GetComponent<BearAnimationDriver>().animator.runtimeAnimatorController)!=Chosen+"/BearMotion.controller")throw new Exception("Selected controller/model identity lost");
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(AssetDatabase.GetAssetPath(skin.sharedMesh)!=Chosen+"/Bear.fbx"||(skin.renderingLayerMask&255u)!=2u)throw new Exception("Selected mesh/layers lost");
                foreach(var mat in skin.sharedMaterials)
                {string path=AssetDatabase.GetAssetPath(mat);if(!path.StartsWith(FleeceStudyBuilder.Output+"/")&&!path.StartsWith(TwilightLookDevBuilder.Output+"/"))throw new Exception("Selected material changed");}
            }
            var set=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Output+"/APV_BakingSet.asset");
            if(!set.HasBakedData()||!set.sceneGUIDs.Contains(AssetDatabase.AssetPathToGUID(ScenePath))||LightmapSettings.lightmaps.Length==0)throw new Exception("Private bake missing");
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/bedside-bear-checks.txt"),"PASS: exact BearFit_v001 mesh/controller/material references; hero rendering layer 2; private GPU lightmaps="+LightmapSettings.lightmaps.Length+"; APV baked and scene membership correct; sources unchanged.\nScene="+ScenePath+"\nAPV loaded="+ProbeReferenceVolume.instance.DataHasBeenLoaded()+"\n");
        }
        [MenuItem("Sorajiro/Bedside Bear/2. Open selected bear in bedside room")]
        public static void Open(){Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Create and bake first");EditorSceneManager.OpenScene(ScenePath);}
        [Serializable]private class PreparationRecord{public string recipe;public string[] files,hashes;}
        [Serializable]private class BakeRecord{public string state,scene,startedUtc,completedUtc,gpu,error;public int lightmaps;public bool apvBaked;public double elapsedSeconds;}
    }
}
