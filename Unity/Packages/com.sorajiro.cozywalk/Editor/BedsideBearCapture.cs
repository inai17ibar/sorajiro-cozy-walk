using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sorajiro.Editor
{
    public static class BedsideBearCapture
    {
        private static RenderTexture target;private static string folder;private static int shot,warmup;private static GameObject lights;private static Renderer[] skins;private static uint[] layers;
        private static readonly string[] Cameras={"S01_Portrait","S02_Gameplay","S03_ThreeQuarter","S05_Bedding","S01_Portrait","S01_Portrait"};
        private static readonly string[] Variants={"Preferred","Preferred","Preferred","Preferred","Hero_OFF","Room_Direct"};
        public static void Begin()
        {
            if(target)throw new Exception("Capture already active");BedsideBearBuilder.Check();
            lights=GameObject.Find(BedsideBearBuilder.LightsName);skins=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>().visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>().Cast<Renderer>().ToArray();layers=skins.Select(r=>r.renderingLayerMask).ToArray();
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/bedside-bear",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(folder);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();shot=warmup=0;EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
        }
        private static void Tick()
        {
            try
            {
                lights.SetActive(Variants[shot]!="Hero_OFF");for(int i=0;i<skins.Length;i++)skins[i].renderingLayerMask=Variants[shot]=="Room_Direct"?(layers[i]&~255u)|1u:layers[i];
                var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name==Cameras[shot]);float aspect=camera.aspect;
                try
                {
                    camera.aspect=16f/9;var request=new RenderPipeline.StandardRequest{destination=target};if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("HDRP request unsupported");RenderPipeline.SubmitRenderRequest(camera,request);
                    if(++warmup<40)return;
                    string name=Cameras[shot]+"_"+Variants[shot];var previous=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
                    var model=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>().visualRoot;var driver=model.parent.GetComponent<BearAnimationDriver>();
                    var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(BedsideBearBuilder.Output+"/LookDevVolume.asset");profile.TryGet<Exposure>(out var exposure);
                    File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Frame{utc=DateTime.UtcNow.ToString("O"),scene=BedsideBearBuilder.ScenePath,variant=Variants[shot],gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,model=model.name,controller=AssetDatabase.GetAssetPath(driver.animator.runtimeAnimatorController),position=camera.transform.position,rotation=camera.transform.eulerAngles,fov=camera.fieldOfView,fixedEV=exposure.fixedExposure.value,lightmaps=LightmapSettings.lightmaps.Length,apvBaked=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(BedsideBearBuilder.Output+"/APV_BakingSet.asset").HasBakedData(),renderingLayer=skins[0].renderingLayerMask&255u,mesh=AssetDatabase.GetAssetPath(((SkinnedMeshRenderer)skins[0]).sharedMesh)},true));
                }
                finally{camera.aspect=aspect;}
                if(++shot==6){File.WriteAllText(Path.Combine(folder,"complete.txt"),"6 actual HDRP/Metal 1920x1080 frames. Exact chosen v001/3 mesh/material/controller; room context differs. Hero_OFF removes dedicated direct lights; Room_Direct changes hero receiving layer to room lighting. Same pose/camera/EV for portrait comparisons. No FPS/owner-quality claim.\nScene="+BedsideBearBuilder.ScenePath+"\n");Cleanup();}else warmup=0;
            }
            catch(Exception e){Cleanup();Debug.LogException(e);}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;if(lights)lights.SetActive(true);
            if(skins!=null)for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].renderingLayerMask=layers[i];
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Frame
        {
            public string utc,scene,variant,gpu,api,unity,model,controller,mesh;public Vector3 position,rotation;public float fov,fixedEV;public int width=1920,height=1080,lightmaps,sourceComparison=3;public uint renderingLayer;public bool apvBaked,dynamicResolution=false;
        }
    }
}
