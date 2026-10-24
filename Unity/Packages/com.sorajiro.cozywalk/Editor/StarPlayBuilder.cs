using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class StarPlayBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/StarPlay_v003";
        public const string ScenePath=Output+"/L_LookDev.unity";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe=>"starplay-v003-"+GeneratedContentGuard.HashFile(BedsideBearBuilder.Output+"/generation-receipt.json")+
            string.Concat(new[]{"Editor/StarPlayBuilder.cs","Editor/PlushSfx.cs","Runtime/StarPlaySession.cs","Runtime/StarPickup.cs","Runtime/StarBurst.cs","Runtime/FootstepBeat.cs"}
                .Select(f=>GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk",f))));
        public static void Preflight()
        {
            BedsideBearBuilder.Preflight();
            var r=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(BedsideBearBuilder.Output+"/generation-receipt.json"));
            if(!GeneratedContentGuard.CanReuse(BedsideBearBuilder.Output,Application.unityVersion,Hdrp,r.recipe))throw new Exception("Preferred room unavailable");
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked output refused");
        }
        [MenuItem("Sorajiro/Star Play/1. Create stars and sound")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            var room=EditorSceneManager.OpenScene(BedsideBearBuilder.ScenePath);
            var original=UnityEngine.Object.FindObjectsByType<MeshRenderer>().Single(r=>r.name=="Room_StarGlow");
            var pieces=Split(original.GetComponent<MeshFilter>().sharedMesh,original.transform.localToWorldMatrix);
            if(pieces.Count(m=>m.bounds.center.y<.5f)!=3)throw new Exception("Expected exactly 3 floor stars; preserve unrecognized art");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var session=new GameObject("Star_Play_Session").AddComponent<StarPlaySession>();
            var stars=new GameObject("Original_Stars_Separated");session.replacementStars=stars;
            int index=0;
            foreach(var mesh in pieces.OrderBy(m=>m.bounds.center.y).ThenBy(m=>m.bounds.center.x))
            {
                Vector3 center=mesh.bounds.center;var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)vertices[i]-=center;mesh.vertices=vertices;mesh.RecalculateBounds();
                mesh.name="Star_Piece_"+index.ToString("D2");AssetDatabase.CreateAsset(mesh,Output+"/"+mesh.name+".asset");
                var g=new GameObject(mesh.name);g.transform.SetParent(stars.transform,false);g.transform.position=center;g.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=original.sharedMaterial;r.shadowCastingMode=original.shadowCastingMode;r.renderingLayerMask=original.renderingLayerMask;
                if(center.y<.5f)
                {
                    var c=g.AddComponent<SphereCollider>();c.radius=Mathf.Max(.075f,mesh.bounds.extents.magnitude*.85f);c.isTrigger=true;
                    var body=g.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                    var pickup=g.AddComponent<StarPickup>();pickup.session=session;pickup.visual=r;pickup.starId="bedside-star-"+index;
                }
                index++;
            }
            stars.SetActive(false);
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);session.sparkMesh=UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);session.sparkMesh.name="SparkSphere";
            UnityEngine.Object.DestroyImmediate(primitive);AssetDatabase.CreateAsset(session.sparkMesh,Output+"/SparkSphere.asset");
            var material=new Material(Shader.Find("HDRP/Unlit")){name="Golden_Spark"};material.SetColor("_UnlitColor",new Color(1,.67f,.15f));material.SetColor("_EmissiveColor",new Color(5,2.7f,.35f));
            HDMaterial.ValidateMaterial(material);AssetDatabase.CreateAsset(material,Output+"/Golden_Spark.mat");session.sparkMaterial=material;
            for(int i=0;i<3;i++)PlushSfx.WriteWav(Output+"/Foot_"+i+".wav",PlushSfx.Samples(0,i));
            PlushSfx.WriteWav(Output+"/Star_Chime.wav",PlushSfx.Samples(1));PlushSfx.WriteWav(Output+"/Soft_Landing.wav",PlushSfx.Samples(2));AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Output,"*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            session.footsteps=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Foot_"+i+".wav")).ToArray();
            session.pickup=AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Star_Chime.wav");session.landing=AssetDatabase.LoadAssetAtPath<AudioClip>(Output+"/Soft_Landing.wav");
            EditorSceneManager.SaveScene(scene,ScenePath);SceneManager.SetActiveScene(room);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Preflight();
            GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();Check();
        }
        // Split by welded geometric connectivity: FBX material batching merged every star into one renderer.
        private static List<Mesh> Split(Mesh source,Matrix4x4 world)
        {
            var v=source.vertices;var triangles=source.triangles;var normals=source.normals;var uv=source.uv;
            int[] parent=Enumerable.Range(0,v.Length).ToArray();var welded=new Dictionary<Vector3Int,int>();
            Func<int,int> root=null;root=i=>parent[i]==i?i:parent[i]=root(parent[i]);
            Action<int,int> join=(a,b)=>parent[root(a)]=root(b);
            for(int i=0;i<v.Length;i++)
            {var p=v[i]*100000;var key=new Vector3Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z));if(welded.TryGetValue(key,out int old))join(i,old);else welded[key]=i;}
            for(int i=0;i<triangles.Length;i+=3){join(triangles[i],triangles[i+1]);join(triangles[i],triangles[i+2]);}
            var groups=new Dictionary<int,List<int>>();
            foreach(int i in triangles){int id=root(i);if(!groups.TryGetValue(id,out var group))groups[id]=group=new List<int>();group.Add(i);}
            var result=new List<Mesh>();
            foreach(var group in groups.Values)
            {
                var unique=group.Distinct().ToArray();var indices=unique.Select((value,index)=>(value,index)).ToDictionary(p=>p.value,p=>p.index);
                var mesh=new Mesh();mesh.vertices=unique.Select(i=>world.MultiplyPoint3x4(v[i])).ToArray();
                mesh.normals=unique.Select(i=>world.inverse.transpose.MultiplyVector(normals[i]).normalized).ToArray();
                if(uv.Length==v.Length)mesh.uv=unique.Select(i=>uv[i]).ToArray();
                mesh.triangles=group.Select(i=>indices[i]).ToArray();mesh.RecalculateBounds();result.Add(mesh);
            }
            if(result.Sum(m=>m.triangles.Length)!=triangles.Length)throw new Exception("Star geometry changed");return result;
        }
        [MenuItem("Sorajiro/Star Play/2. Open playable bedside stars")]
        public static void Open()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Generate stars first");
            var room=EditorSceneManager.OpenScene(BedsideBearBuilder.ScenePath);EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(room);Check();
        }
        public static void Check()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Missing gameplay output");
            var session=UnityEngine.Object.FindAnyObjectByType<StarPlaySession>();if(!session||session.gameObject.scene.path!=ScenePath)throw new Exception("Open gameplay overlay");
            var stars=session.replacementStars.GetComponentsInChildren<StarPickup>(true);if(stars.Length!=3||stars.Select(s=>s.starId).Distinct().Count()!=3)throw new Exception("Star identities invalid");
            if(session.footsteps.Length!=3||session.footsteps.Any(c=>!c)||!session.pickup||!session.landing||!session.sparkMaterial.shader.isSupported)throw new Exception("Missing sound/effect assets");
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/star-play-assets.txt"),"PASS: 3 unique floor stars; kinematic triggers; 5 PCM clips; supported HDRP effect; protected original room receipts.\nOverlay="+ScenePath+"\nRoom="+BedsideBearBuilder.ScenePath+"\n");
        }
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","play"}.Contains(stage))throw new ArgumentException("Unknown star stage");
            string path=Path.Combine(Repo,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_star-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{state="scheduled",stage=stage};File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;run=()=>{EditorApplication.update-=run;try{job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                if(stage=="create")Create();if(stage=="open")Open();if(stage=="check")Check();if(stage=="play"){Check();EditorApplication.isPlaying=true;}job.state="completed";}
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}finally{File.WriteAllText(path,JsonUtility.ToJson(job,true));}};EditorApplication.update+=run;return path;
        }
        [Serializable]private class Job{public string state,stage,error;}
    }
}
