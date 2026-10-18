using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorajiro.Editor
{
    // Local Pipeline bridge. Long asset operations outlive Pipeline's 5s main-thread RPC limit.
    // A scheduled response is never treated as completion; the job file records the outcome.
    public static class ArtReviewAutomation
    {
        public static string Schedule(string stage)
        {
            if(stage!="create" && stage!="edit-tests" && stage!="runtime-tests")throw new ArgumentException("Unknown review stage");
            string folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs");Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_"+stage+".json");
            var job=new Job{stage=stage,startedUtc=DateTime.UtcNow.ToString("O"),state="scheduled",scene=TwilightLookDevBuilder.ScenePath};
            File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;
            run=()=>
            {
                EditorApplication.update-=run;
                try
                {
                    job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                    if(stage=="create")TwilightLookDevBuilder.Create();
                    if(stage=="edit-tests")
                    {TwilightVerification.SkinTest();BearAnimationVerification.ClipTests();BearAnimationVerification.SoleTests();TwilightVerification.SurfaceTest();}
                    if(stage=="runtime-tests")RunScriptedTests();
                    job.state="completed";
                }
                catch(Exception e){job.state="failed";job.error=e.ToString();Debug.LogException(e);}
                finally{job.completedUtc=DateTime.UtcNow.ToString("O");File.WriteAllText(path,JsonUtility.ToJson(job,true));}
            };
            EditorApplication.update+=run;
            return path;
        }

        private static void RunScriptedTests()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play first");
            var motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();
            bool focused=motor.Focused;
            try
            {
                // Explicit test fixture: remote CLI has no keyboard focus. Gameplay focus handling is unchanged.
                motor.SendMessage("OnApplicationFocus",true);
                MotorSmokeTests.Run();BearAnimationVerification.RuntimeTests();VerificationTools.RuntimeStatus();
                File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/twilight-scripted-focus.txt"),
                    "Scripted integration test supplied focused=true, restoring previous focus afterward. Actual production motor, collision and Animator; not a keyboard/focus-device acceptance test.\nScene="+TwilightLookDevBuilder.ScenePath+"\n");
            }
            finally{if(motor)motor.SendMessage("OnApplicationFocus",focused);}
        }

        // Keep the explicit test fixture focused only for this 30s automated capture.
        private static SorajiroMotor captureMotor;
        private static bool previousFocus;
        public static void RecordScriptedMotion()
        {
            if(captureMotor)throw new InvalidOperationException("Scripted capture already active");
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play first");
            captureMotor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();previousFocus=captureMotor.Focused;
            try
            {
                captureMotor.SendMessage("OnApplicationFocus",true);TwilightMotionCapture.Begin();
                EditorApplication.update+=MaintainCaptureFixture;AssemblyReloadEvents.beforeAssemblyReload+=ReleaseCaptureFixture;
                Debug.Log("SORAJIRO SCRIPTED CAPTURE: test fixture supplies focus=true; no keyboard/focus acceptance claim.");
            }
            catch{ReleaseCaptureFixture();throw;}
        }
        private static void MaintainCaptureFixture()
        {
            if(!EditorApplication.isPlaying || !UnityEngine.Object.FindAnyObjectByType<MotionCaptureProbe>()){ReleaseCaptureFixture();return;}
            if(captureMotor)captureMotor.SendMessage("OnApplicationFocus",true);
        }
        private static void ReleaseCaptureFixture()
        {
            EditorApplication.update-=MaintainCaptureFixture;AssemblyReloadEvents.beforeAssemblyReload-=ReleaseCaptureFixture;
            if(captureMotor)captureMotor.SendMessage("OnApplicationFocus",previousFocus);captureMotor=null;
        }
        [Serializable]private class Job{public string stage,startedUtc,completedUtc,state,scene,error;}
    }
}
