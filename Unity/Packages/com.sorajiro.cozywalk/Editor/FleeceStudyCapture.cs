using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class FleeceStudyCapture
    {
        private static RenderTexture target;private static string folder;private static int frame,warmup,oldVariant;
        private static FleeceVariantPreview preview;private static GameObject patch;private static Renderer patchFibers;private static Material oldPatchMaterial;
        private static bool room,oldMotorEnabled,oldAnimatorEnabled;private static SorajiroMotor motor;private static Animator animator;
        private static readonly string[] StudioShots={"F01_Portrait","F01_Portrait","F01_Portrait","F02_FullBody","F02_FullBody","F02_FullBody","F03_Surface","F03_Surface","F03_Surface","F04_Patch","F04_Patch"};
        private static readonly string[] StudioVariants={"Baseline","Fabric","Groomed","Baseline","Fabric","Groomed","Baseline","Fabric","Groomed","Patch_FabricMesh","Patch_HairRibbons"};
        private static readonly string[] RoomShots={"S01_Portrait","S01_Portrait","S03_ThreeQuarter","S03_ThreeQuarter"};
        private static readonly string[] RoomVariants={"Baseline","Groomed","Baseline","Groomed"};
        public static void Capture(bool inRoom=false)
        {
            if(target)throw new Exception("Capture already active");room=inRoom;
            if(room){if(!EditorApplication.isPlaying)throw new Exception("Room preview requires Play");}
            else FleeceStudyBuilder.Check();
            preview=UnityEngine.Object.FindAnyObjectByType<FleeceVariantPreview>();if(!preview)throw new Exception("Fleece preview missing");oldVariant=preview.variant;
            if(room)
            {
                motor=UnityEngine.Object.FindAnyObjectByType<SorajiroMotor>();animator=motor.GetComponent<BearAnimationDriver>().animator;
                oldMotorEnabled=motor.enabled;oldAnimatorEnabled=animator.enabled;motor.enabled=false;animator.enabled=false;
            }
            else
            {
                patch=Resources.FindObjectsOfTypeAll<Transform>().Single(t=>t.gameObject.scene==SceneManager.GetActiveScene() && t.name=="Fleece_Patch_Comparison").gameObject;
                patchFibers=patch.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Patch_Fibers");oldPatchMaterial=patchFibers.sharedMaterial;
            }
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/fleece",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+(room?"_room":"_studio"));Directory.CreateDirectory(folder);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();
            frame=0;warmup=0;EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            Debug.Log("SORAJIRO FLEECE CAPTURE START: "+folder);
        }
        private static void Tick()
        {
            try
            {
                if(!preview || (room && !EditorApplication.isPlaying))throw new Exception("Capture interrupted");
                var shots=room?RoomShots:StudioShots;var variants=room?RoomVariants:StudioVariants;string variant=variants[frame];
                preview.Apply(variant=="Baseline"?0:variant=="Fabric"?1:2);
                if(patch)
                {
                    bool show=shots[frame]=="F04_Patch";patch.SetActive(show);preview.body.enabled=!show;preview.pile.enabled=!show;
                    patchFibers.sharedMaterial=variant=="Patch_HairRibbons"?AssetDatabase.LoadAssetAtPath<Material>(FleeceStudyBuilder.Output+"/Patch_Hair.mat"):oldPatchMaterial;
                }
                var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name==shots[frame]);float aspect=camera.aspect;
                try
                {
                    camera.aspect=16f/9;var request=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("HDRP request not supported");RenderPipeline.SubmitRenderRequest(camera,request);
                    if(++warmup<40)return;
                    var previous=RenderTexture.active;var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);string name=shots[frame]+"_"+variant;
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
                    File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Metadata{utc=DateTime.UtcNow.ToString("O"),scene=SceneManager.GetActiveScene().path,variant=variant,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,roomPreview=room,position=camera.transform.position,rotation=camera.transform.eulerAngles,fov=camera.fieldOfView,bodyShader=preview.body.sharedMaterials[0].shader.name,pileShader=preview.pile.sharedMaterials[0].shader.name,pileVertices=preview.pile.sharedMesh.vertexCount,patchShader=patchFibers?patchFibers.sharedMaterial.shader.name:null},true));
                }
                finally{camera.aspect=aspect;}
                if(++frame==shots.Length){File.WriteAllText(Path.Combine(folder,"complete.txt"),"Actual HDRP/Metal capture. "+shots.Length+" fixed 1920x1080 frames. Same frozen pose and lighting for variants. No image generation, no FPS or owner-quality claim.\nScene="+SceneManager.GetActiveScene().path+"\n");Cleanup();Debug.Log("SORAJIRO FLEECE CAPTURE COMPLETE: "+folder);}else warmup=0;
            }
            catch(Exception e){Cleanup();Debug.LogException(e);}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(preview){preview.Apply(oldVariant);preview.body.enabled=true;preview.pile.enabled=true;}
            if(patch){patch.SetActive(false);patchFibers.sharedMaterial=oldPatchMaterial;}
            if(motor)motor.enabled=oldMotorEnabled;if(animator)animator.enabled=oldAnimatorEnabled;motor=null;animator=null;patch=null;patchFibers=null;
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Metadata
        {
            public string utc,scene,variant,gpu,api,unity,bodyShader,pileShader,patchShader;public Vector3 position,rotation;public float fov;public int width=1920,height=1080,pileVertices;public bool roomPreview,dynamicResolution=false;
            public string scope="Actual Editor HDRP StandardRequest/TAA; no blur or generated-image validation. Studio EV7, room retains original EV6. Different lighting contexts are not an isolated material comparison.";
        }
    }
}
