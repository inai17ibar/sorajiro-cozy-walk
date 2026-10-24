using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sorajiro.Editor
{
    public static class FleeceLightingCapture
    {
        private static FleeceLighting rig;
        private static SorajiroMotor motor;
        private static BearAnimationDriver driver;
        private static Animator animator;
        private static bool motorEnabled,driverEnabled,animatorEnabled;
        private static Camera close;
        private static RenderTexture target;
        private static string folder,path;
        private static int shot,warmup;
        private static readonly string[] Cameras={"S01_Portrait","S03_ThreeQuarter","S02_Gameplay","Fleece_Close"};
        public static string Begin()
        {
            rig=UnityEngine.Object.FindAnyObjectByType<FleeceLighting>();
            if(target||!EditorApplication.isPlaying||!rig||!rig.Ready)throw new Exception("Ready lighting Play required");
            var session=UnityEngine.Object.FindAnyObjectByType<StarPlaySession>();motor=session.Motor;driver=motor.GetComponent<BearAnimationDriver>();animator=driver.animator;
            motorEnabled=motor.enabled;driverEnabled=driver.enabled;animatorEnabled=animator.enabled;
            motor.ReturnToStart();motor.enabled=driver.enabled=false;animator.Play("Locomotion",0,0);animator.SetFloat("Speed",0);animator.Update(0);animator.enabled=false;
            close=UnityEngine.Object.Instantiate(UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name=="S01_Portrait"));close.name="Fleece_Close";close.enabled=false;close.tag="Untagged";
            Vector3 face=motor.transform.position+new Vector3(0,.30f,-.04f);close.transform.position=face+Vector3.back*.45f;close.transform.LookAt(face);close.fieldOfView=32;
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/fleece-light",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(folder);
            path=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/PipelineJobs",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"_light-capture.json");File.WriteAllText(path,JsonUtility.ToJson(new Job{state="running",folder=folder},true));
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();shot=warmup=0;
            EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;return path;
        }
        private static void Tick()
        {
            try
            {
                int variant=shot/4;rig.Apply(variant);
                var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name==Cameras[shot%4]);float aspect=camera.aspect;camera.aspect=16f/9;
                try
                {
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(++warmup<40)return;
                    string name=camera.name+"_"+new[]{"Before","Side600","Side850"}[variant];var old=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
                    finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);}
                    var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(BedsideBearBuilder.Output+"/LookDevVolume.asset");profile.TryGet<Exposure>(out var exposure);
                    var lights=GameObject.Find(BedsideBearBuilder.LightsName).GetComponentsInChildren<Light>().Concat(new[]{rig.sideKey}).Select(l=>new Lamp{name=l.name,enabled=l.enabled,power=l.intensity,unit=l.lightUnit.ToString(),position=l.transform.position,forward=l.transform.forward}).ToArray();
                    File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Frame{variant=variant,utc=DateTime.UtcNow.ToString("O"),overlay=FleeceLightingBuilder.ScenePath,room=BedsideBearBuilder.ScenePath,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),position=camera.transform.position,rotation=camera.transform.eulerAngles,fov=camera.fieldOfView,exposure=exposure.fixedExposure.value,model=motor.visualRoot.name,lights=lights},true));
                }
                finally{camera.aspect=aspect;}
                warmup=0;if(++shot<12)return;
                File.WriteAllText(Path.Combine(folder,"complete.txt"),"12 actual Metal/HDRP 1080p frames, 4 fixed cameras x 3 direct-light variants, same model/materials/pose/EV7/room baked lighting. No image generation or performance claim.\n");
                File.WriteAllText(path,JsonUtility.ToJson(new Job{state="completed",folder=folder},true));Cleanup();
            }
            catch(Exception e){File.WriteAllText(path,JsonUtility.ToJson(new Job{state="failed",folder=folder,error=e.ToString()},true));Cleanup();Debug.LogException(e);}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(rig)rig.Apply(1);if(motor)motor.enabled=motorEnabled;if(driver)driver.enabled=driverEnabled;if(animator)animator.enabled=animatorEnabled;
            if(close)UnityEngine.Object.DestroyImmediate(close.gameObject);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Job{public string state,folder,error;}
        [Serializable]private class Lamp{public string name,unit;public bool enabled;public float power;public Vector3 position,forward;}
        [Serializable]private class Frame{public string utc,overlay,room,gpu,api,model;public int variant,width=1920,height=1080;public Vector3 position,rotation;public float fov,exposure;public Lamp[] lights;}
    }
}
