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
    public static class FleeceLightingBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/FleeceLighting_v002";
        public const string ScenePath=Output+"/L_LookDev.unity";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe=>"fleece-light-v002-"+GeneratedContentGuard.HashFile(StarPlayBuilder.Output+"/generation-receipt.json")+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Runtime/FleeceLighting.cs"))+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/FleeceLightingBuilder.cs"));
        public static void Preflight()
        {
            StarPlayBuilder.Preflight();
            var r=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(StarPlayBuilder.Output+"/generation-receipt.json"));
            if(!GeneratedContentGuard.CanReuse(StarPlayBuilder.Output,Application.unityVersion,Hdrp,r.recipe))throw new Exception("Protected Star Play input unavailable");
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked lighting output refused");
        }
        [MenuItem("Sorajiro/Fleece Lighting/1. Create soft side lighting")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var rig=new GameObject("Fleece_Side_Lighting").AddComponent<FleeceLighting>();
            var light=new GameObject("Soft_Grazing_Fleece_Key").AddComponent<Light>();light.transform.SetParent(rig.transform,false);var hd=light.gameObject.AddComponent<HDAdditionalLightData>();
            light.type=LightType.Rectangle;light.lightUnit=LightUnit.Lumen;light.intensity=600;light.areaSize=new Vector2(.28f,.45f);light.color=new Color(.94f,.97f,1);
            light.range=4;light.shadows=LightShadows.Soft;light.lightmapBakeType=LightmapBakeType.Realtime;
            hd.SetLightLayer((UnityEngine.Rendering.HighDefinition.RenderingLayerMask)2u,(UnityEngine.Rendering.HighDefinition.RenderingLayerMask)3u);
            light.enabled=false;rig.sideKey=light;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);SceneManager.SetActiveScene(original);AssetDatabase.Refresh();Preflight();
            GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();
        }
        [MenuItem("Sorajiro/Fleece Lighting/2. Open soft-lit star room")]
        public static void Open()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Create lighting first");
            StarPlayBuilder.Open();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        }
        public static void Check()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Lighting output missing");
            var rig=UnityEngine.Object.FindAnyObjectByType<FleeceLighting>();if(!rig||rig.gameObject.scene.path!=ScenePath||!rig.sideKey)throw new Exception("Open lighting overlay");
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/fleece-light-assets.txt"),"PASS: protected room and Star Play receipts; separate side-light scene; model, materials, exposure and baked lighting unchanged.\n"+ScenePath+"\n");
        }
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","play"}.Contains(stage))throw new ArgumentException("Unknown fleece-light stage");
            string path=Path.Combine(Repo,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_light-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{stage=stage,state="scheduled"};File.WriteAllText(path,JsonUtility.ToJson(job,true));EditorApplication.CallbackFunction run=null;
            run=()=>{EditorApplication.update-=run;try{job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                if(stage=="create")Create();if(stage=="open")Open();if(stage=="check")Check();if(stage=="play"){Check();EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}job.state="completed";}
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}finally{File.WriteAllText(path,JsonUtility.ToJson(job,true));}};EditorApplication.update+=run;return path;
        }
        [Serializable]private class Job{public string stage,state,error;}
    }
}
