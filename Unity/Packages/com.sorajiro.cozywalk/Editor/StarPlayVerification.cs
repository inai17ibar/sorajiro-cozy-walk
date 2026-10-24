using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sorajiro.Editor
{
    public static class StarPlayVerification
    {
        private static IEnumerator routine;
        private static StarPlaySession session;
        private static SorajiroMotor motor;
        private static string folder,jobPath;
        private static bool priorFocus,priorMute,priorMotorEnabled;
        private static float priorVolume;
        private static int assertions;
        private static double deadline;
        private static RenderTexture target;
        private static Transform priorView;
        private static Camera evidenceCamera;
        public static string Begin()
        {
            if(routine!=null||!EditorApplication.isPlaying)throw new Exception("Fresh gameplay Play required");
            session=UnityEngine.Object.FindAnyObjectByType<StarPlaySession>();
            if(!session||!session.Ready||session.CollectedCount!=0)throw new Exception("Start a fresh Star Play session");
            motor=session.Motor;priorFocus=motor.Focused;priorVolume=session.Volume;priorMute=session.Muted;
            priorView=motor.view;
            priorMotorEnabled=motor.enabled;motor.enabled=false; // Scripted Step fixture owns input, not keyboard Update.
            motor.SendMessage("OnApplicationFocus",true);session.SetVolume(.65f,false);assertions=0;
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/star-play",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(folder);
            jobPath=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_star-runtime.json");
            File.WriteAllText(jobPath,"{\"state\":\"running\"}");deadline=EditorApplication.timeSinceStartup+90;
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();
            routine=Run();EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;return jobPath;
        }
        private static void Assert(bool value,string message){assertions++;if(!value)throw new Exception(message);}
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Gameplay verification timed out");
                if(!EditorApplication.isPlaying)throw new Exception("Play stopped during verification");
                if(!routine.MoveNext()){Cleanup();}
            }
            catch(Exception e){File.WriteAllText(jobPath,JsonUtility.ToJson(new Result{state="failed",error=e.ToString(),folder=folder},true));Cleanup();Debug.LogException(e);}
        }
        private static IEnumerator Run()
        {
            Assert(session.TotalCount==3,"Expected 3 reachable floor stars");Assert(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(a=>a.isActiveAndEnabled)==1,"One active listener required");
            var stars=session.replacementStars.GetComponentsInChildren<StarPickup>().OrderBy(s=>s.transform.position.z).ToArray();
            var sourceCamera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name=="S03_ThreeQuarter");
            evidenceCamera=UnityEngine.Object.Instantiate(sourceCamera);evidenceCamera.name="Star_Event_Evidence";evidenceCamera.enabled=false;evidenceCamera.tag="Untagged";
            evidenceCamera.transform.position=stars[0].transform.position+new Vector3(-.65f,.4f,-.85f);
            evidenceCamera.transform.LookAt(stars[0].transform.position+Vector3.up*.06f);evidenceCamera.fieldOfView=55;
            var controller=motor.GetComponent<CharacterController>();var originalView=motor.view;motor.view=null;
            var footProbe=session.FootAudio.gameObject.AddComponent<GameplayAudioProbe>();footProbe.Begin(AudioSettings.outputSampleRate*2*8);
            var chimeProbe=session.PickupAudio.gameObject.AddComponent<GameplayAudioProbe>();chimeProbe.Begin(AudioSettings.outputSampleRate*2*8);
            var wrong=new GameObject("NonBear_Test_Collider").AddComponent<BoxCollider>();Assert(!stars[0].TryCollect(wrong),"Non-bear cannot collect");UnityEngine.Object.Destroy(wrong.gameObject);
            foreach(var star in stars)Assert(star.visual.enabled&&!star.Collected,"Fresh stars must be visible");
            for(int i=0;i<40;i++){Render("Before",false);yield return null;}Render("Before",true);
            var first=stars[0];controller.enabled=false;motor.transform.position=first.transform.position+Vector3.back*.32f-Vector3.up*first.transform.position.y;controller.enabled=true;
            // Advance the actual controller through the real trigger. Never invoke OnTriggerEnter manually.
            double end=EditorApplication.timeSinceStartup+1.25;
            while(EditorApplication.timeSinceStartup<end && session.CollectedCount==0)
            {motor.Step(0,1,false,false,Mathf.Min(.033f,Time.deltaTime));yield return null;}
            Assert(first.Collected && session.CollectedCount==1 && !first.visual.enabled && !first.GetComponent<Collider>().enabled,"Actual controller contact must collect/hide/disable exactly once");
            Assert(first.associatedLights.All(l=>!l.enabled),"Nearby star direct lights switch off on pickup");
            Assert(!first.TryCollect(controller)&&session.CollectedCount==1,"Duplicate contact must not count twice");
            end=EditorApplication.timeSinceStartup+.18;while(EditorApplication.timeSinceStartup<end)yield return null;
            Render("Contact_Burst",true);Assert(UnityEngine.Object.FindObjectsByType<StarBurst>().Length>0,"Pickup must produce live effect");
            Assert(session.PickupSoundCount==1,"One chime per pickup");
            motor.ReturnToStart();Assert(session.CollectedCount==1&&!first.visual.enabled,"R preserves collected stars");
            end=EditorApplication.timeSinceStartup+1.3;while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Time.deltaTime);yield return null;}
            Assert(UnityEngine.Object.FindObjectsByType<StarBurst>().Length==0,"Burst cleans up after its lifetime");
            int idle=session.FootstepCount;end=EditorApplication.timeSinceStartup+.45;
            while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Time.deltaTime);yield return null;}Assert(session.FootstepCount==idle,"Idle must not play footsteps");
            motor.ReturnToStart();int before=session.FootstepCount;
            File.WriteAllText(Path.Combine(folder,"walk.csv"),"speed,grounded,phase,locomotion,transition,footsteps\n");
            end=EditorApplication.timeSinceStartup+1.4;while(EditorApplication.timeSinceStartup<end)
            {
                motor.Step(1,0,false,false,Mathf.Min(.033f,Time.deltaTime));yield return null;
                var state=motor.GetComponent<BearAnimationDriver>().animator.GetCurrentAnimatorStateInfo(0);
                File.AppendAllText(Path.Combine(folder,"walk.csv"),FormattableString.Invariant($"{motor.PlanarSpeed},{motor.Grounded},{state.normalizedTime},{state.IsName("Locomotion")},{motor.GetComponent<BearAnimationDriver>().animator.IsInTransition(0)},{session.FootstepCount}\n"));
            }
            Assert(session.FootstepCount>before,"Actual walk and Animator must produce foot beats");int walkSteps=session.FootstepCount-before;
            int land=session.LandingCount;motor.Step(0,0,true,false,.016f);yield return null;Assert(!motor.Grounded,"Jump must leave the floor");int airSteps=session.FootstepCount;
            end=EditorApplication.timeSinceStartup+.12;while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Mathf.Min(.02f,Time.deltaTime));yield return null;}
            Assert(session.FootstepCount==airSteps,"No footsteps in air");
            end=EditorApplication.timeSinceStartup+.8;while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Mathf.Min(.02f,Time.deltaTime));yield return null;}
            Assert(motor.Grounded&&session.LandingCount>land,"Landing must play a soft impact");
            int stopped=session.FootstepCount;motor.SendMessage("OnApplicationFocus",false);
            end=EditorApplication.timeSinceStartup+.25;while(EditorApplication.timeSinceStartup<end)yield return null;
            Assert(!session.IsActive&&session.FootstepCount==stopped,"Unfocused state suspends gameplay audio and collection");
            motor.SendMessage("OnApplicationFocus",true);
            session.SetVolume(.23f,true);Assert(session.Muted&&session.FootAudio.volume==0&&session.PickupAudio.volume==0,"Mute affects both sound sources");
            Assert(Mathf.Abs(PlayerPrefs.GetFloat("Sorajiro.Sfx.Volume.v1")-.23f)<.001f&&PlayerPrefs.GetInt("Sorajiro.Sfx.Mute.v1")==1,"Sound settings persist locally");session.SetVolume(.65f,false);
            int runStart=session.FootstepCount;motor.ReturnToStart();end=EditorApplication.timeSinceStartup+.7;
            while(EditorApplication.timeSinceStartup<end){motor.Step(-1,0,false,true,Mathf.Min(.033f,Time.deltaTime));yield return null;}
            Assert(session.FootstepCount>runStart,"Run must produce foot beats");
            foreach(var star in stars.Where(s=>!s.Collected))
            {
                controller.enabled=false;motor.transform.position=new Vector3(star.transform.position.x,0,star.transform.position.z-.25f);controller.enabled=true;
                end=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<end&&!star.Collected){motor.Step(0,1,false,false,Mathf.Min(.033f,Time.deltaTime));yield return null;}
                Assert(star.Collected,"Each floor star must be reachable by walking");
            }
            Assert(session.CollectedCount==3&&session.PickupSoundCount==3,"All 3 stars, exactly 3 chimes");motor.ReturnToStart();
            end=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Time.deltaTime);yield return null;}
            for(int i=0;i<40;i++){Render("After",false);yield return null;}Render("After",true);
            var foot=footProbe.Snapshot(out int footChannels);var chime=chimeProbe.Snapshot(out int chimeChannels);
            float footPeak=foot.Length==0?0:foot.Max(v=>Mathf.Abs(v)),chimePeak=chime.Length==0?0:chime.Max(v=>Mathf.Abs(v));
            Assert(footChannels>0&&footPeak>.0001f&&footPeak<1,"Footstep audio DSP must contain finite audible-range samples");Assert(chimeChannels>0&&chimePeak>.0001f&&chimePeak<1,"Pickup audio DSP must contain nonzero samples");
            Assert(foot.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))&&chime.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)),"No invalid audio samples");
            PlushSfx.WriteWav(Path.Combine(folder,"Actual_Foot_DSP.wav"),foot,footChannels,AudioSettings.outputSampleRate);PlushSfx.WriteWav(Path.Combine(folder,"Actual_Chime_DSP.wav"),chime,chimeChannels,AudioSettings.outputSampleRate);
            motor.view=originalView;
            var result=new Result{state="completed",folder=folder,assertions=assertions,collected=session.CollectedCount,footsteps=session.FootstepCount,walkSteps=walkSteps,landings=session.LandingCount,chimes=session.PickupSoundCount,footPeak=footPeak,chimePeak=chimePeak,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),lightmaps=LightmapSettings.lightmaps.Length};
            File.WriteAllText(jobPath,JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"checks.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"complete.txt"),"PASS: actual controller trigger collection, duplicate/nonbear guards, R retention, 3 reachable stars, live burst and cleanup, Animator steps/run/air/landing, focus/mute/settings, actual output-source DSP samples. Scripted input fixture; no hardware listening, Player build or FPS claim.\nOverlay="+StarPlayBuilder.ScenePath+"\nRoom="+BedsideBearBuilder.ScenePath+"\n");
        }
        private static void Render(string name,bool save)
        {
            var camera=evidenceCamera;float aspect=camera.aspect;camera.aspect=16f/9;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(!save)return;
                var previous=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
                finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
                File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Frame{utc=DateTime.UtcNow.ToString("O"),overlay=StarPlayBuilder.ScenePath,room=BedsideBearBuilder.ScenePath,position=camera.transform.position,rotation=camera.transform.eulerAngles,fov=camera.fieldOfView,collected=session.CollectedCount,sparks=UnityEngine.Object.FindObjectsByType<StarBurst>().Length,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),model=motor.visualRoot.name},true));
            }
            finally{camera.aspect=aspect;}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;routine=null;
            if(motor){motor.SendMessage("OnApplicationFocus",priorFocus);motor.view=priorView;motor.enabled=priorMotorEnabled;}if(session)session.SetVolume(priorVolume,priorMute);
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
            if(evidenceCamera)UnityEngine.Object.DestroyImmediate(evidenceCamera.gameObject);
        }
        [Serializable]private class Result{public string state,error,folder,gpu,api;public int assertions,collected,footsteps,walkSteps,landings,chimes,lightmaps;public float footPeak,chimePeak;}
        [Serializable]private class Frame{public string utc,overlay,room,gpu,api,model;public Vector3 position,rotation;public float fov;public int collected,sparks,width=1920,height=1080;}
    }
}
