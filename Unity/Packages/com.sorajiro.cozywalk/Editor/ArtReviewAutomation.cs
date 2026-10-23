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
            if(stage!="create" && stage!="edit-tests" && stage!="runtime-tests" && stage!="corner-create" && stage!="corner-check" && stage!="corner-runtime" && stage!="corner-open" && stage!="corner-capture" && stage!="corner-play" && stage!="bedding-create" && stage!="bedding-open" && stage!="bedding-check" && stage!="bedding-capture" && stage!="bedding-play" && stage!="bedding-runtime" && stage!="contact-create" && stage!="contact-open" && stage!="contact-check" && stage!="contact-capture" && stage!="contact-play" && stage!="contact-runtime" && stage!="drape-create" && stage!="drape-open" && stage!="drape-check" && stage!="drape-capture" && stage!="drape-play" && stage!="drape-runtime" && stage!="surface-create" && stage!="surface-open" && stage!="surface-check" && stage!="surface-capture" && stage!="surface-play" && stage!="surface-runtime" && stage!="round-create" && stage!="round-open" && stage!="round-check" && stage!="round-capture" && stage!="round-play" && stage!="round-runtime" && stage!="stable-create" && stage!="stable-open" && stage!="stable-check" && stage!="stable-capture" && stage!="stable-play" && stage!="stable-runtime")throw new ArgumentException("Unknown review stage");
            string folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs");Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_"+stage+".json");
            var job=new Job{stage=stage,startedUtc=DateTime.UtcNow.ToString("O"),state="scheduled",scene=stage.StartsWith("stable-")?BeddingStableBuilder.ScenePath:stage.StartsWith("round-")?BeddingRoundBuilder.ScenePath:stage.StartsWith("surface-")?BeddingSurfaceBuilder.ScenePath:stage.StartsWith("drape-")?BeddingDrapeBuilder.ScenePath:stage.StartsWith("contact-")?BeddingContactBuilder.ScenePath:stage.StartsWith("bedding-")?BeddingRefinementBuilder.ScenePath:stage.StartsWith("corner-")?BedsideCornerBuilder.ScenePath:TwilightLookDevBuilder.ScenePath};
            File.WriteAllText(path,JsonUtility.ToJson(job,true));
            EditorApplication.CallbackFunction run=null;
            run=()=>
            {
                EditorApplication.update-=run;
                try
                {
                    job.state="running";File.WriteAllText(path,JsonUtility.ToJson(job,true));
                    if(stage=="create")TwilightLookDevBuilder.Create();
                    if(stage=="stable-create")BeddingStableBuilder.Create();
                    if(stage=="stable-open")BeddingStableBuilder.Open();
                    if(stage=="stable-check")BeddingStableBuilder.Check();
                    if(stage=="stable-capture"){BedsideCornerCapture.Capture();job.note="Invocation only; wait for 7 PNGs and complete.txt";}
                    if(stage=="stable-play"){BeddingStableBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="round-create")BeddingRoundBuilder.Create();
                    if(stage=="round-open")BeddingRoundBuilder.Open();
                    if(stage=="round-check")BeddingRoundBuilder.Check();
                    if(stage=="round-capture"){BedsideCornerCapture.Capture();job.note="Invocation only; wait for 7 PNGs and complete.txt";}
                    if(stage=="round-play"){BeddingRoundBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="surface-create")BeddingSurfaceBuilder.Create();
                    if(stage=="surface-open")BeddingSurfaceBuilder.Open();
                    if(stage=="surface-check")BeddingSurfaceBuilder.Check();
                    if(stage=="surface-capture"){BedsideCornerCapture.Capture();job.note="Invocation only; wait for 7 PNGs and complete.txt";}
                    if(stage=="surface-play"){BeddingSurfaceBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="drape-create")BeddingDrapeBuilder.Create();
                    if(stage=="drape-open")BeddingDrapeBuilder.Open();
                    if(stage=="drape-check")BeddingDrapeBuilder.Check();
                    if(stage=="drape-capture"){BedsideCornerCapture.Capture();job.note="Invocation only; wait for 7 PNGs and complete.txt";}
                    if(stage=="drape-play"){BeddingDrapeBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="contact-create")BeddingContactBuilder.Create();
                    if(stage=="contact-open")BeddingContactBuilder.Open();
                    if(stage=="contact-check")BeddingContactBuilder.Check();
                    if(stage=="contact-capture"){BedsideCornerCapture.Capture();job.note="Invocation only; wait for 7 PNGs and complete.txt";}
                    if(stage=="contact-play"){BeddingContactBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="bedding-create")BeddingRefinementBuilder.Create();
                    if(stage=="bedding-open")BeddingRefinementBuilder.Open();
                    if(stage=="bedding-check")BeddingRefinementBuilder.Check();
                    if(stage=="bedding-capture"){BedsideCornerCapture.Capture();job.note="Capture invocation only; wait for 7 frames and complete.txt";}
                    if(stage=="bedding-play"){BeddingRefinementBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="corner-create")BedsideCornerBuilder.Create();
                    if(stage=="corner-check")BedsideCornerBuilder.Check();
                    if(stage=="corner-open")BedsideCornerBuilder.Open();
                    if(stage=="corner-play"){BedsideCornerBuilder.Check();EditorApplication.isPlaying=true;}
                    if(stage=="corner-capture"){BedsideCornerCapture.Capture();job.note="Capture started asynchronously; wait for the capture folder's complete.txt. This job is only the method invocation.";}
                    if(stage=="edit-tests")
                    {TwilightVerification.SkinTest();BearAnimationVerification.ClipTests();BearAnimationVerification.SoleTests();TwilightVerification.SurfaceTest();}
                    if(stage=="runtime-tests" || stage=="corner-runtime" || stage=="bedding-runtime" || stage=="contact-runtime" || stage=="drape-runtime" || stage=="surface-runtime" || stage=="round-runtime" || stage=="stable-runtime")RunScriptedTests();
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
                bool corner=motor.gameObject.scene.path==BedsideCornerBuilder.ScenePath;
                File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/"+((motor.gameObject.scene.path==BeddingRefinementBuilder.ScenePath || motor.gameObject.scene.path==BeddingContactBuilder.ScenePath || motor.gameObject.scene.path==BeddingDrapeBuilder.ScenePath || motor.gameObject.scene.path==BeddingSurfaceBuilder.ScenePath || motor.gameObject.scene.path==BeddingRoundBuilder.ScenePath || motor.gameObject.scene.path==BeddingStableBuilder.ScenePath)?"bedding":corner?"bedside":"twilight")+"-scripted-focus.txt"),
                    "Scripted integration test supplied focused=true, restoring previous focus afterward. Actual production motor, collision and Animator; not a keyboard/focus-device acceptance test.\nScene="+motor.gameObject.scene.path+"\n");
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
        [MenuItem("Sorajiro/Bedside/4. Open baked corner")]
        public static void OpenCorner(){BedsideCornerBuilder.Open();}
        [MenuItem("Sorajiro/Bedside/5. Check persisted baked data")]
        public static void CheckCorner(){BedsideCornerBuilder.Check();}
        [MenuItem("Sorajiro/Bedding/3. Capture actual comparisons")]
        public static void CaptureBedding(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding/4. Open baked bedding")]
        public static void OpenBedding(){BeddingRefinementBuilder.Open();}
        [MenuItem("Sorajiro/Bedding/5. Check persisted bedding")]
        public static void CheckBedding(){BeddingRefinementBuilder.Check();}
        [MenuItem("Sorajiro/Bedding Contact/3. Capture actual comparisons")]
        public static void CaptureContact(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding Contact/4. Open baked bedding")]
        public static void OpenContact(){BeddingContactBuilder.Open();}
        [MenuItem("Sorajiro/Bedding Contact/5. Check persisted bedding")]
        public static void CheckContact(){BeddingContactBuilder.Check();}
        [MenuItem("Sorajiro/Bedding Drape/3. Capture actual comparisons")]
        public static void CaptureDrape(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding Drape/4. Open baked bedding")]
        public static void OpenDrape(){BeddingDrapeBuilder.Open();}
        [MenuItem("Sorajiro/Bedding Drape/5. Check persisted bedding")]
        public static void CheckDrape(){BeddingDrapeBuilder.Check();}
        [MenuItem("Sorajiro/Bedding Surface/3. Capture actual comparisons")]
        public static void CaptureSurface(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding Surface/4. Open baked bedding")]
        public static void OpenSurface(){BeddingSurfaceBuilder.Open();}
        [MenuItem("Sorajiro/Bedding Surface/5. Check persisted bedding")]
        public static void CheckSurface(){BeddingSurfaceBuilder.Check();}
        [MenuItem("Sorajiro/Bedding Round/3. Capture actual comparisons")]
        public static void CaptureRound(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding Round/4. Open baked bedding")]
        public static void OpenRound(){BeddingRoundBuilder.Open();}
        [MenuItem("Sorajiro/Bedding Round/5. Check persisted bedding")]
        public static void CheckRound(){BeddingRoundBuilder.Check();}
        [MenuItem("Sorajiro/Bedding Final/3. Capture latest comparisons")]
        public static void CaptureStable(){BedsideCornerCapture.Capture();}
        [MenuItem("Sorajiro/Bedding Final/4. Open latest baked bedding")]
        public static void OpenStable(){BeddingStableBuilder.Open();}
        [MenuItem("Sorajiro/Bedding Final/5. Check latest persisted bedding")]
        public static void CheckStable(){BeddingStableBuilder.Check();}
        [Serializable]private class Job{public string stage,startedUtc,completedUtc,state,scene,error,note;}
    }
}
