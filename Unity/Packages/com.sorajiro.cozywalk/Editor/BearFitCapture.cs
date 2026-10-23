using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sorajiro.Editor
{
    public static class BearFitCapture
    {
        private static BearFitPreview fit;private static RenderTexture target;private static string folder;
        private static int shot,warmup,previous;
        private static readonly string[] Cameras={"F05_FrontFullBody","F01_Portrait","F03_Surface","F02_FullBody"};
        private static readonly string[] Variants={"Baseline_Fleece2","Relit_Fleece2","Fuller_Relit_Fleece2"};
        [MenuItem("Sorajiro/Bear Fit/3. Capture fixed comparisons")]
        public static void Begin()
        {
            if(target)throw new Exception("Capture active");BearFitBuilder.Check();
            fit=UnityEngine.Object.FindAnyObjectByType<BearFitPreview>();previous=fit.variant;
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/bear-fit",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(folder);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();shot=warmup=0;
            EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
        }
        private static void Tick()
        {
            try
            {
                int variant=shot%3;fit.Apply(variant);var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name==Cameras[shot/3]);float aspect=camera.aspect;
                try
                {
                    camera.aspect=16f/9;var request=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("HDRP request unavailable");RenderPipeline.SubmitRenderRequest(camera,request);
                    if(++warmup<40)return;
                    var old=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);string name=Cameras[shot/3]+"_"+Variants[variant];
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
                    finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);}
                    var model=variant==2?fit.currentModel:fit.baselineModel;
                    var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
                    File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Metadata{utc=DateTime.UtcNow.ToString("O"),scene=BearFitBuilder.ScenePath,variant=Variants[variant],mode=variant,fixedExposure=variant==0?7:6.3f,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,model=model.name,modelAsset=variant==2?BearFitBuilder.Output+"/Bear.fbx":FleeceStudyBuilder.Output+"/Bear.fbx",cameraPosition=camera.transform.position,cameraRotation=camera.transform.eulerAngles,fov=camera.fieldOfView,pileVertices=skins.Single(r=>r.name.Contains("Pile")).sharedMesh.vertexCount,bodyShader=skins.Single(r=>!r.name.Contains("Pile")).sharedMaterials[0].shader.name},true));
                }
                finally{camera.aspect=aspect;}
                if(++shot==12){File.WriteAllText(Path.Combine(folder,"complete.txt"),"12 actual fixed HDRP/Metal 1920x1080 TAA frames. All modes use the owner's preferred Fleece v005 material variant 2. EV7 baseline; EV6.3 relit. Lighting comparisons include exposure. No generated-image, FPS or owner-quality claim.\nScene="+BearFitBuilder.ScenePath+"\n");Cleanup();}else warmup=0;
            }
            catch(Exception e){Cleanup();Debug.LogException(e);}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(fit)fit.Apply(previous);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Metadata
        {
            public string utc,scene,variant,gpu,api,unity,model,modelAsset,bodyShader;public int mode,width=1920,height=1080,pileVertices,retainedFleeceVariant=2;public Vector3 cameraPosition,cameraRotation;public float fov,fixedExposure=7;public bool dynamicResolution=false;
        }
    }
}
