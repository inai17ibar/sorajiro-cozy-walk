using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sorajiro.Editor
{
    public static class ComfortPlayVerification
    {
        private static ComfortPlay play;private static StarPlaySession session;private static SorajiroMotor motor;private static OrbitCamera orbit;
        private static bool focused,motorEnabled,orbitEnabled,muted;private static float volume;private static Transform view;
        private static GameObject wall;private static GameplayAudioProbe footProbe,jumpProbe;private static RenderTexture target;
        private static IEnumerator routine;private static string folder,path;private static double deadline;private static int assertions;
        public static string Begin()
        {
            if(routine!=null||!EditorApplication.isPlaying||EditorApplication.isPaused)throw new Exception("Fresh unpaused comfort Play required");
            play=UnityEngine.Object.FindAnyObjectByType<ComfortPlay>();session=UnityEngine.Object.FindAnyObjectByType<StarPlaySession>();
            if(!play||!play.Ready||session.CollectedCount!=0)throw new Exception("Start a fresh ready comfort session");
            motor=session.Motor;orbit=play.CameraRig;focused=motor.Focused;motorEnabled=motor.enabled;orbitEnabled=orbit.enabled;view=motor.view;volume=session.Volume;muted=session.Muted;
            motor.SendMessage("OnApplicationFocus",true);motor.enabled=orbit.enabled=false;session.SetVolume(.65f,false);motor.ReturnToStart();orbit.Follow(0);
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/comfort-play",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(folder);
            path=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_comfort-runtime.json");File.WriteAllText(path,"{\"state\":\"running\"}");
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();assertions=0;deadline=EditorApplication.timeSinceStartup+90;
            routine=Run();EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;return path;
        }
        private static void Assert(bool value,string message){assertions++;if(!value)throw new Exception(message);}
        private static void Tick()
        {
            try{if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new Exception("Comfort test interrupted/timed out");if(!routine.MoveNext())Cleanup();}
            catch(Exception e){File.WriteAllText(path,JsonUtility.ToJson(new Result{state="failed",error=e.ToString(),folder=folder,assertions=assertions},true));Cleanup();Debug.LogException(e);}
        }
        private static IEnumerator Run()
        {
            Assert(orbit.comfortControls&&Mathf.Abs(orbit.pitch-26)<.01f&&Mathf.Abs(orbit.distance-1.25f)<.01f,"Overhead camera defaults");
            Assert(orbit.target==motor&&motor.view==orbit.transform,"Actual follow target and camera-relative movement references must be connected");
            Assert(session.footsteps.SequenceEqual(play.cuteSteps)&&session.landing==play.cuteLanding&&play.cuteJump,"Actual game must use cute clips");
            Assert(UnityEngine.Object.FindAnyObjectByType<FleeceLighting>().Variant==1,"Selected lighting remains active");
            footProbe=session.FootAudio.gameObject.AddComponent<GameplayAudioProbe>();footProbe.Begin(AudioSettings.outputSampleRate*2*12);
            jumpProbe=play.JumpAudio.gameObject.AddComponent<GameplayAudioProbe>();jumpProbe.Begin(AudioSettings.outputSampleRate*2*4);
            for(int i=0;i<20;i++){motor.Step(0,0,false,false,1f/60);orbit.Follow(1f/60);yield return null;}
            orbit.comfortControls=false;orbit.pitch=3;orbit.distance=1.2f;orbit.Follow(0);
            for(int i=0;i<40;i++){Render("Before_Low_Camera",false);yield return null;}Render("Before_Low_Camera",true);
            orbit.ConfigureComfort();for(int i=0;i<40;i++){orbit.Follow(1f/60);Render("Overhead_Default",false);yield return null;}Render("Overhead_Default",true);
            Assert(orbit.transform.position.y>motor.transform.position.y+.6f,"Camera actually sits above the bear");
            foreach(float height in new[]{.01f,.40f}){var p=Camera.main.WorldToViewportPoint(motor.transform.position+Vector3.up*height);Assert(p.z>0&&p.x>.08f&&p.x<.92f&&p.y>.05f&&p.y<.95f,"Bear stays in viewport");}
            float yaw=orbit.yaw;orbit.TurnStep(1);Assert(Mathf.Abs(Mathf.DeltaAngle(yaw,orbit.yaw)-45)<.01f,"E/button requests exactly 45 degrees");
            for(int i=0;i<40;i++){orbit.Follow(1f/60);Render("Turn_Right",false);yield return null;}Render("Turn_Right",true);
            Assert(Mathf.Abs(Mathf.DeltaAngle(orbit.transform.eulerAngles.y,orbit.yaw))<.2f,"Visible camera reaches requested angle");
            orbit.TurnStep(-1);Assert(Mathf.Abs(Mathf.DeltaAngle(yaw,orbit.yaw))<.01f,"Q/button reverses the turn");
            orbit.Recenter();Assert(Mathf.Abs(Mathf.DeltaAngle(orbit.yaw,motor.visualRoot.eulerAngles.y))<.01f,"C/button returns behind bear");
            for(int i=0;i<40;i++){orbit.Follow(1f/60);Render("Behind_Bear",false);yield return null;}Render("Behind_Bear",true);
            float frozenYaw=orbit.yaw;var frozenPose=orbit.transform.position;motor.SendMessage("OnApplicationFocus",false);orbit.TurnStep(1);orbit.Recenter();orbit.Follow(.2f);
            Assert(orbit.yaw==frozenYaw&&orbit.transform.position==frozenPose,"No unfocused camera input/drift");motor.SendMessage("OnApplicationFocus",true);
            motor.ReturnToStart();orbit.Follow(0);Assert(Mathf.Abs(orbit.pitch-26)<.01f,"R restores readable pitch");
            orbit.yaw=0;for(int i=0;i<50;i++)orbit.Follow(1f/60);
            Vector3 pivot=motor.transform.position+Vector3.up*.25f,backward=Quaternion.Euler(26,0,0)*Vector3.back;
            wall=new GameObject("Temporary_Camera_Collision_Test");wall.transform.position=pivot+backward*.65f;var box=wall.AddComponent<BoxCollider>();box.size=new Vector3(2,2,.08f);Physics.SyncTransforms();orbit.Follow(1f/60);
            Assert(orbit.ActualDistance<.7f,"Real wall shortens camera distance");Assert(!Physics.CheckSphere(orbit.transform.position,.035f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Camera sphere stays outside colliders");
            UnityEngine.Object.DestroyImmediate(wall);wall=null;Physics.SyncTransforms();orbit.Follow(1f/60);Assert(orbit.ActualDistance>1,"Camera recovers after obstacle removal");
            motor.ReturnToStart();orbit.Follow(0);motor.view=orbit.transform;Vector3 start=motor.transform.position,forward=Vector3.ProjectOnPlane(orbit.transform.forward,Vector3.up).normalized;
            for(int i=0;i<12;i++){motor.Step(0,1,false,false,1f/60);orbit.Follow(1f/60);yield return null;}
            Assert(Vector3.Dot(Vector3.ProjectOnPlane(motor.transform.position-start,Vector3.up).normalized,forward)>.99f,"W follows actual camera direction");
            int feet=session.FootstepCount;double end=EditorApplication.timeSinceStartup+1.3;
            while(EditorApplication.timeSinceStartup<end){motor.Step(0,1,false,false,Mathf.Min(Time.deltaTime,.033f));orbit.Follow(Time.deltaTime);yield return null;}Assert(session.FootstepCount>feet,"Cute footsteps with actual animation");
            for(int i=0;i<5;i++){motor.Step(0,0,false,false,1f/60);yield return null;}
            jumpProbe.Begin(AudioSettings.outputSampleRate*2*4);
            int hops=play.JumpSoundCount;motor.Step(0,0,true,false,.016f);
            end=EditorApplication.timeSinceStartup+.3;while(EditorApplication.timeSinceStartup<end&&play.JumpSoundCount==hops)yield return null;
            Assert(!motor.Grounded&&play.JumpSoundCount==hops+1,"One cute sound on actual takeoff");
            int airFeet=session.FootstepCount;motor.Step(0,0,true,false,.016f);yield return null;Assert(play.JumpSoundCount==hops+1,"No duplicate sound on air-jump request");
            int lands=session.LandingCount;end=EditorApplication.timeSinceStartup+1;
            while(EditorApplication.timeSinceStartup<end){motor.Step(0,0,false,false,Mathf.Min(Time.deltaTime,.02f));orbit.Follow(Time.deltaTime);yield return null;}
            Assert(motor.Grounded&&session.LandingCount>lands,"Cute sound on actual landing");Assert(session.FootstepCount==airFeet,"No extra footsteps while idle/airborne");
            session.SetVolume(.21f,true);end=EditorApplication.timeSinceStartup+.3;while(EditorApplication.timeSinceStartup<end&&play.JumpAudio.volume!=0)yield return null;
            Assert(play.JumpAudio.volume==0&&session.FootAudio.volume==0,"Mute includes jump sound");session.SetVolume(.65f,false);yield return null;
            motor.ReturnToStart();orbit.Follow(0);yield return null;Assert(play.JumpSoundCount==hops+1,"R cannot produce false hop sound");
            var star=session.replacementStars.GetComponentsInChildren<StarPickup>().First(s=>!s.Collected);var controller=motor.GetComponent<CharacterController>();motor.view=null;
            controller.enabled=false;motor.transform.position=new Vector3(star.transform.position.x,0,star.transform.position.z-.28f);controller.enabled=true;
            end=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<end&&!star.Collected){motor.Step(0,1,false,false,Mathf.Min(Time.deltaTime,.033f));yield return null;}
            Assert(star.Collected&&session.CollectedCount>=1,"Real trigger collection still works");int count=session.CollectedCount;motor.ReturnToStart();orbit.Follow(0);Assert(session.CollectedCount==count,"R retains collected stars");
            var foot=footProbe.Snapshot(out int footChannels);var jump=jumpProbe.Snapshot(out int jumpChannels);float footPeak=foot.Length>0?foot.Max(v=>Mathf.Abs(v)):0,jumpPeak=jump.Length>0?jump.Max(v=>Mathf.Abs(v)):0;
            Assert(footChannels>0&&footPeak>.0001f&&footPeak<1,"Actual cute foot/land DSP");Assert(jumpChannels>0&&jumpPeak>.0001f&&jumpPeak<1,"Actual cute jump DSP");
            PlushSfx.WriteWav(Path.Combine(folder,"Cute_Foot_And_Land_DSP.wav"),foot,footChannels,AudioSettings.outputSampleRate);PlushSfx.WriteWav(Path.Combine(folder,"Cute_Jump_DSP.wav"),jump,jumpChannels,AudioSettings.outputSampleRate);
            var result=new Result{state="completed",folder=folder,assertions=assertions,jumps=play.JumpSoundCount,feet=session.FootstepCount,landings=session.LandingCount,collected=session.CollectedCount,footPeak=footPeak,jumpPeak=jumpPeak,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString()};
            File.WriteAllText(Path.Combine(folder,"checks.json"),JsonUtility.ToJson(result,true));File.WriteAllText(path,JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"complete.txt"),"PASS: overhead camera, camera-relative movement, turns/recenter, actual wall collision, focus/reset, actual cute foot/jump/landing DSP and collection retention. Scripted Step/control fixture; no hardware listening/Player/FPS claim.\nOverlay="+ComfortPlayBuilder.ScenePath+"\n");
        }
        private static void Render(string name,bool save)
        {
            var camera=Camera.main;float aspect=camera.aspect;camera.aspect=16f/9;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(!save)return;var old=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);}
                File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Frame{utc=DateTime.UtcNow.ToString("O"),overlay=ComfortPlayBuilder.ScenePath,position=camera.transform.position,rotation=camera.transform.eulerAngles,fov=camera.fieldOfView,requestedPitch=orbit.pitch,requestedYaw=orbit.yaw,actualDistance=orbit.ActualDistance,model=motor.visualRoot.name,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString()},true));
            }
            finally{camera.aspect=aspect;}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;routine=null;if(wall)UnityEngine.Object.DestroyImmediate(wall);
            if(motor){motor.SendMessage("OnApplicationFocus",focused);motor.enabled=motorEnabled;motor.view=view;}if(orbit)orbit.enabled=orbitEnabled;if(session)session.SetVolume(volume,muted);
            if(footProbe)UnityEngine.Object.Destroy(footProbe);if(jumpProbe)UnityEngine.Object.Destroy(jumpProbe);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Result{public string state,error,folder,gpu,api;public int assertions,jumps,feet,landings,collected;public float footPeak,jumpPeak;}
        [Serializable]private class Frame{public string utc,overlay,model,gpu,api;public Vector3 position,rotation;public float fov,requestedPitch,requestedYaw,actualDistance;public int width=1920,height=1080;}
    }
}
