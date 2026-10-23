using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class BearFitActions
    {
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","clips","capture","play","runtime"}.Contains(stage))throw new ArgumentException("Unknown bear fit action");
            string path=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_fit-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{stage=stage,state="scheduled",startedUtc=DateTime.UtcNow.ToString("O")};File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;run=()=>
            {
                EditorApplication.update-=run;
                try
                {
                    job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                    switch(stage)
                    {
                        case "create":BearFitBuilder.Create();break;
                        case "open":BearFitBuilder.Open();break;
                        case "check":BearFitBuilder.Check();break;
                        case "clips":BearFitBuilder.Check();BearAnimationVerification.ClipTestsFor(BearFitBuilder.Output);BearAnimationVerification.SoleTestsFor(BearFitBuilder.Output);break;
                        case "capture":BearFitCapture.Begin();job.note="Invocation only; wait for 12 PNGs and complete.txt";break;
                        case "play":BearFitBuilder.Check();EditorApplication.isPlaying=true;break;
                        case "runtime":RuntimeTests();break;
                    }
                    job.state="completed";
                }
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}
                finally{job.completedUtc=DateTime.UtcNow.ToString("O");job.scene=SceneManager.GetActiveScene().path;File.WriteAllText(path,JsonUtility.ToJson(job,true));}
            };EditorApplication.update+=run;return path;
        }
        private static void RuntimeTests()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=BearFitBuilder.ScenePath)throw new Exception("Enter fit Play first");
            var fit=UnityEngine.Object.FindAnyObjectByType<BearFitPreview>();bool focus=fit.motor.Focused;int previous=fit.variant;
            try
            {
                for(int i=0;i<3;i++)
                {
                    fit.Apply(i);
                    if(fit.currentModel.activeSelf!=(i==2)||fit.baselineModel.activeSelf!=(i!=2)||fit.currentLights.activeSelf!=(i!=0)||fit.baselineLights.activeSelf!=(i==0))throw new Exception("Comparison activation mismatch");
                    if(fit.motor.visualRoot!=(i==2?fit.currentModel:fit.baselineModel).transform || fit.driver.animator!=(i==2?fit.currentAnimator:fit.baselineAnimator))throw new Exception("Active motor/animator binding mismatch");
                }
                fit.Apply(2);fit.motor.SendMessage("OnApplicationFocus",true);RestContact(fit);MotorSmokeTests.Run();BearAnimationVerification.RuntimeTests();
                File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/bear-fit-variant-runtime.txt"),"PASS: production Apply switches models/lights/motor visual/Animator correctly in all modes. New rest-pose clips active.\nScene="+BearFitBuilder.ScenePath+"\n");
            }
            finally{fit.Apply(previous);fit.motor.SendMessage("OnApplicationFocus",focus);}
        }
        private static void RestContact(BearFitPreview fit)
        {
            fit.motor.ReturnToStart();Physics.SyncTransforms();
            for(int i=0;i<60;i++)fit.motor.Step(0,0,false,false,1f/60);
            fit.driver.Sync(1f/60);fit.currentAnimator.Play("Locomotion",0,0);fit.currentAnimator.Update(0);
            var skin=fit.currentModel.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>!r.name.Contains("Pile"));var mesh=new Mesh();
            try
            {
                skin.BakeMesh(mesh);float bottom=mesh.vertices.Min(p=>skin.transform.TransformPoint(p).y);
                var floor=GameObject.Find("Studio_Floor").GetComponent<Collider>();float gap=bottom-floor.bounds.max.y;
                if(!float.IsFinite(gap)||gap<-.005f||gap>.015f)throw new Exception("Resting foot/floor gap outside trial tolerance: "+gap);
                File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/bear-fit-rest-contact.txt"),"PASS: actual settled CharacterController + sampled Idle skin. Bare cloth/sole minimum above floor="+gap.ToString("F6")+"m. This geometry measurement does not approve contact-shadow quality or live foot sliding.\nScene="+BearFitBuilder.ScenePath+"\n");
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);fit.motor.ReturnToStart();}
        }
        [Serializable]private class Job{public string stage,state,startedUtc,completedUtc,scene,error,note;}
    }
}
