using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class ComfortPlayBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/ComfortPlay_v002";
        public const string ScenePath=Output+"/L_LookDev.unity";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe=>"comfort-play-v002-"+GeneratedContentGuard.HashFile(FleeceLightingBuilder.Output+"/generation-receipt.json")+
            string.Concat(new[]{"Runtime/ComfortPlay.cs","Runtime/OrbitCamera.cs","Runtime/CameraComfortPolicy.cs","Editor/CutePlushSfx.cs","Editor/PlushSfx.cs","Editor/ComfortPlayBuilder.cs"}
                .Select(f=>GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk",f))));
        public static void Preflight()
        {
            FleeceLightingBuilder.Preflight();
            var r=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(FleeceLightingBuilder.Output+"/generation-receipt.json"));
            if(!GeneratedContentGuard.CanReuse(FleeceLightingBuilder.Output,Application.unityVersion,Hdrp,r.recipe))throw new Exception("Protected lighting input unavailable");
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked comfort output refused");
        }
        [MenuItem("Sorajiro/Play Comfort/1. Create cute sound and camera controls")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            for(int i=0;i<3;i++)PlushSfx.WriteWav(Output+"/Cute_Foot_"+i+".wav",CutePlushSfx.Samples(0,i));
            PlushSfx.WriteWav(Output+"/Cute_Jump.wav",CutePlushSfx.Samples(1));PlushSfx.WriteWav(Output+"/Cute_Landing.wav",CutePlushSfx.Samples(2));AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Output,"*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var play=new GameObject("Cute_Audio_And_Comfort_Camera").AddComponent<ComfortPlay>();
            play.cuteSteps=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Cute_Foot_"+i+".wav")).ToArray();
            play.cuteJump=AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Cute_Jump.wav");play.cuteLanding=AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Cute_Landing.wav");
            EditorSceneManager.SaveScene(scene,ScenePath);SceneManager.SetActiveScene(prior);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Preflight();
            GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();Check();
        }
        [MenuItem("Sorajiro/Play Comfort/2. Open overhead camera and cute sounds")]
        public static void Open()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Create comfort gameplay first");
            FleeceLightingBuilder.Open();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);Check();
        }
        public static void Check()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Comfort assets changed/missing");
            var play=UnityEngine.Object.FindAnyObjectByType<ComfortPlay>();if(!play||play.gameObject.scene.path!=ScenePath||play.cuteSteps.Length!=3||play.cuteSteps.Any(c=>!c)||!play.cuteJump||!play.cuteLanding)throw new Exception("Open generated comfort overlay with its clips");
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/comfort-play-assets.txt"),"PASS: protected bear, room, Star Play and side lighting; separate comfort overlay; 5 imported PCM sound clips.\nOverlay="+ScenePath+"\n");
        }
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","play"}.Contains(stage))throw new ArgumentException("Unknown comfort stage");
            string path=Path.Combine(Repo,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_comfort-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{stage=stage,state="scheduled"};File.WriteAllText(path,JsonUtility.ToJson(job,true));EditorApplication.CallbackFunction run=null;
            run=()=>{EditorApplication.update-=run;try{job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                if(stage=="create")Create();if(stage=="open")Open();if(stage=="check")Check();if(stage=="play"){Check();EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}job.state="completed";}
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}finally{File.WriteAllText(path,JsonUtility.ToJson(job,true));}};EditorApplication.update+=run;return path;
        }
        [Serializable]private class Job{public string stage,state,error;}
    }
}
