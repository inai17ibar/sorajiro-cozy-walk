using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class FleeceStudyActions
    {
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","capture","play","runtime","room-preview","room-capture"}.Contains(stage))throw new ArgumentException("Unknown fleece action");
            string path=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_fleece-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{stage=stage,state="scheduled",startedUtc=DateTime.UtcNow.ToString("O")};File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;run=()=>
            {
                EditorApplication.update-=run;
                try
                {
                    job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                    switch(stage)
                    {
                        case "create":FleeceStudyBuilder.Create();break;
                        case "open":FleeceStudyBuilder.Open();break;
                        case "check":FleeceStudyBuilder.Check();break;
                        case "capture":FleeceStudyCapture.Capture();job.note="Invocation only; wait for 11 PNGs and complete.txt";break;
                        case "play":FleeceStudyBuilder.Check();EditorApplication.isPlaying=true;break;
                        case "runtime":RuntimeTests();break;
                        case "room-preview":RoomPreview();break;
                        case "room-capture":FleeceStudyCapture.Capture(true);job.note="Invocation only; wait for 4 PNGs and complete.txt";break;
                    }
                    job.state="completed";
                }
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}
                finally{job.completedUtc=DateTime.UtcNow.ToString("O");job.scene=SceneManager.GetActiveScene().path;File.WriteAllText(path,JsonUtility.ToJson(job,true));}
            };EditorApplication.update+=run;return path;
        }
        public static void RuntimeTests()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=FleeceStudyBuilder.ScenePath)throw new Exception("Enter fleece Play first");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();bool focus=motor.Focused;
            try{motor.SendMessage("OnApplicationFocus",true);MotorSmokeTests.Run();BearAnimationVerification.RuntimeTests();VerifyVariants();}
            finally{motor.SendMessage("OnApplicationFocus",focus);}
        }
        private static void VerifyVariants()
        {
            var preview=UnityEngine.Object.FindAnyObjectByType<FleeceVariantPreview>();int previous=preview.variant;
            try
            {
                for(int i=0;i<3;i++)
                {
                    preview.Apply(i);
                    if(preview.variant!=i || preview.pile.sharedMesh!=(i==2?preview.groomedPile:preview.baselinePile))throw new Exception("Variant mesh mismatch");
                    if(!preview.body.sharedMaterials.SequenceEqual(i==0?preview.baselineBodyMaterials:preview.fleeceBodyMaterials) || !preview.pile.sharedMaterials.SequenceEqual(i==0?preview.baselinePileMaterials:preview.fleecePileMaterials))throw new Exception("Variant materials mismatch");
                }
                var mesh=preview.groomedPile;
                foreach(var p in mesh.vertices)if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new Exception("Non-finite groom vertex");
                foreach(var w in mesh.boneWeights)
                {
                    float sum=w.weight0+w.weight1+w.weight2+w.weight3;
                    if(!float.IsFinite(sum)||Mathf.Abs(sum-1)>.001f)throw new Exception("Invalid groom skin weights");
                    if(w.boneIndex0<0||w.boneIndex1<0||w.boneIndex2<0||w.boneIndex3<0||w.boneIndex0>=preview.pile.bones.Length||w.boneIndex1>=preview.pile.bones.Length||w.boneIndex2>=preview.pile.bones.Length||w.boneIndex3>=preview.pile.bones.Length)throw new Exception("Invalid groom bone index");
                }
                File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/fleece-variant-runtime.txt"),"PASS: all three production Apply variants; exact mesh/material references; finite groom positions; normalized skin weights and valid indices.\nScene="+SceneManager.GetActiveScene().path+"\n");
            }
            finally{preview.Apply(previous);}
        }
        public static void RoomPreview()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=BeddingStableBuilder.ScenePath)throw new Exception("Enter Play in the preserved Bedside_v009 first");
            FleeceStudyBuilder.VerifyAssets();if(UnityEngine.Object.FindAnyObjectByType<FleeceVariantPreview>())throw new Exception("Preview already attached");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();var skins=motor.visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            var pile=skins.Single(r=>r.name.Contains("Pile"));var body=skins.Single(r=>r!=pile);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(FleeceStudyBuilder.Output+"/GroomedShortPile.asset");
            if(mesh.bindposes.Length!=pile.sharedMesh.bindposes.Length)throw new Exception("Room rig bind count differs");
            var a=mesh.bindposes;var b=pile.sharedMesh.bindposes;
            for(int i=0;i<a.Length;i++)for(int j=0;j<16;j++)if(Mathf.Abs(a[i][j]-b[i][j])>.00001f)throw new Exception("Room bind poses differ; refuse preview");
            var preview=motor.gameObject.AddComponent<FleeceVariantPreview>();preview.body=body;preview.pile=pile;preview.baselinePile=pile.sharedMesh;preview.groomedPile=mesh;
            preview.baselineBodyMaterials=body.sharedMaterials;preview.baselinePileMaterials=pile.sharedMaterials;
            preview.fleeceBodyMaterials=body.sharedMaterials.Select(m=>m.name=="BlueFleece" || m.name=="WhiteApplique"?AssetDatabase.LoadAssetAtPath<Material>(FleeceStudyBuilder.Output+"/"+m.name+"_Fleece.mat"):m).ToArray();
            preview.fleecePileMaterials=pile.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(FleeceStudyBuilder.Output+"/"+m.name+"_Fleece.mat")).ToArray();preview.Apply(2);
            File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/fleece-room-preview.txt"),"Actual Play-only room preview; original scene/assets not saved. Bind matrices match within 1e-5. New "+FleeceStudyBuilder.Output+" materials + laid ribbon mesh, original controller and rig.\nScene="+SceneManager.GetActiveScene().path+"\n");
        }
        [MenuItem("Sorajiro/Fleece/2. Open study")]public static void Open(){FleeceStudyBuilder.Open();}
        [MenuItem("Sorajiro/Fleece/3. Capture comparisons")]public static void Capture(){FleeceStudyCapture.Capture();}
        [MenuItem("Sorajiro/Fleece/4. Preview in preserved room (Play only)")]public static void PreviewRoom(){RoomPreview();}
        [Serializable]private class Job{public string stage,state,startedUtc,completedUtc,scene,error,note;}
    }
}
