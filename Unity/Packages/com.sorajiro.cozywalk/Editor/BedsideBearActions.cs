using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class BedsideBearActions
    {
        public static string Schedule(string stage)
        {
            if(!new[]{"create","open","check","capture","play","runtime"}.Contains(stage))throw new ArgumentException("Unknown selected-bear stage");
            string path=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_bedbear-"+stage+".json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            var job=new Job{stage=stage,state="scheduled",startedUtc=DateTime.UtcNow.ToString("O")};File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;run=()=>
            {
                EditorApplication.update-=run;
                try
                {
                    job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                    switch(stage)
                    {
                        case "create":BedsideBearBuilder.Create();break;
                        case "open":BedsideBearBuilder.Open();break;
                        case "check":BedsideBearBuilder.Check();break;
                        case "capture":BedsideBearCapture.Begin();job.note="Invocation only; wait for 6 PNGs and complete.txt";break;
                        case "play":BedsideBearBuilder.Check();EditorApplication.isPlaying=true;break;
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
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=BedsideBearBuilder.ScenePath)throw new Exception("Enter selected-bear room Play");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();bool focus=motor.Focused;
            try{motor.SendMessage("OnApplicationFocus",true);MotorSmokeTests.Run();BearAnimationVerification.RuntimeTests();}
            finally{motor.SendMessage("OnApplicationFocus",focus);}
        }
        [Serializable]private class Job{public string stage,state,startedUtc,completedUtc,scene,error,note;}
    }
}
