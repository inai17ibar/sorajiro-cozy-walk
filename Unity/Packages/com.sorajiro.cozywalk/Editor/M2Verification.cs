using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class M2Verification
    {
        private static readonly string[] Shots={"S01_Portrait","S02_Gameplay","S03_ThreeQuarter","S04_Contact","S02_Gameplay","S05_PilePatch_Diagnostic","S05_PilePatch_Diagnostic"};
        private static string folder;
        private static int shot, warmup;
        private static RenderTexture target;
        private static GameObject patch;
        private static bool oldPatchActive;

        [MenuItem("Sorajiro/M2/4. Capture 4 fixed shots and patch-off (Edit, 1080p)")]
        public static void Capture()
        {
            M2LookDevBuilder.Preflight(); RequireScene();
            if(target) throw new InvalidOperationException("Capture already running.");
            folder=Path.Combine(M2LookDevBuilder.Repository,"Artifacts/Visual/current",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            if(Directory.Exists(folder)) throw new IOException("Capture revision already exists.");
            Directory.CreateDirectory(folder);
            patch=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Single(t=>t.name=="ShortPile_BACK_HEAD_PATCH_TRIAL").gameObject;
            oldPatchActive=patch.activeSelf; patch.SetActive(true);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){name="SorajiroFixed1080p",useDynamicScale=false};
            target.Create(); shot=0; warmup=0;
            EditorApplication.update+=Tick;
            AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            Debug.Log("SORAJIRO M2 CAPTURE started: "+folder+"; actual HDRP StandardRequest; not offline/path-traced or performance evidence.");
        }

        private static void Tick()
        {
            try
            {
                RequireScene();
                if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Capture interrupted by Play mode.");
                Camera camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name==Shots[shot]);
                float previousAspect=camera.aspect;
                try
                {
                    camera.aspect=16f/9;
                    var request=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request)) throw new InvalidOperationException("Active pipeline does not support StandardRequest.");
                    RenderPipeline.SubmitRenderRequest(camera,request);
                    if(++warmup<24) return;
                    string id=Shots[shot]+(patch.activeSelf?"_patch_ON":"_patch_OFF");
                    var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false,false);
                    RenderTexture previous=RenderTexture.active;
                    try {RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,id+".png"),texture.EncodeToPNG());}
                    finally {RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(texture);}
                    var hd=camera.GetComponent<HDAdditionalCameraData>();
                    var data=new ShotEvidence{utc=DateTime.UtcNow.ToString("O"),shot=id,unity=Application.unityVersion,
                        gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),
                        position=camera.transform.position,rotation=camera.transform.eulerAngles,verticalFov=camera.fieldOfView,
                        dynamicResolution=hd.allowDynamicResolution,aa=hd.antialiasing.ToString(),
                        dlss=hd.allowDeepLearningSuperSampling,fsr2=hd.allowFidelityFX2SuperResolution,patchActive=patch.activeSelf};
                    File.WriteAllText(Path.Combine(folder,id+".json"),JsonUtility.ToJson(data,true)+"\n");
                }
                finally {camera.aspect=previousAspect;}
                if(++shot==Shots.Length)
                {
                    File.WriteAllText(Path.Combine(folder,"complete.txt"),"7 actual HDRP request frames saved. Verify PNG dimensions and pixels. No motion/performance/quality approval.\n");
                    Debug.Log("SORAJIRO M2 CAPTURE COMPLETE: "+folder); Cleanup();
                }
                else {warmup=0; patch.SetActive(shot!=4 && shot!=6);}
            }
            catch(Exception e){Debug.LogException(e); Cleanup();}
        }

        private static void Cleanup()
        {
            EditorApplication.update-=Tick; AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(patch) patch.SetActive(oldPatchActive);
            if(target){target.Release(); UnityEngine.Object.DestroyImmediate(target); target=null;}
        }

        [MenuItem("Sorajiro/M2/5. Test imported skin and UV (Edit)")]
        public static void SkinTest()
        {
            M2LookDevBuilder.Preflight(); RequireScene();
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(M2LookDevBuilder.Output+"/LookDevVolume.asset");
            if(profile.components.Count!=9 || profile.components.Any(c=>!c || !EditorUtility.IsPersistent(c)))
                throw new Exception("Volume components were not saved as real subassets.");
            var volume=UnityEngine.Object.FindObjectsByType<Volume>().Single(v=>v.sharedProfile==profile);
            VolumeManager.instance.Update(Camera.main.transform,1);
            var stack=VolumeManager.instance.stack;
            if(stack.GetComponent<Exposure>().fixedExposure.value!=8 || stack.GetComponent<VolumetricClouds>().enable.value)
                throw new Exception("Effective lookdev Volume settings incorrect.");
            var body=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>()
                .Single(r=>r.name=="Bear_SkinnedBody_ArtTrial");
            if(body.sharedMesh.uv.Length!=body.sharedMesh.vertexCount) throw new Exception("Missing imported UVs.");
            if(body.bones.Length<7) throw new Exception("Missing full-body skin bones.");
            if(body.sharedMaterials.Any(m=>!m || m.shader.name!="HDRP/Lit")) throw new Exception("Incorrect HDRP material remap.");
            var before=new Mesh(); var after=new Mesh();
            body.BakeMesh(before); Vector3[] original=before.vertices;
            string result="PASS: real imported FBX UVs, generic skin, HDRP remapped materials.\n";
            try
            {
                foreach(string name in new[]{"head","arm_L","leg_L"})
                {
                    Transform bone=body.bones.Single(b=>b.name==name); Quaternion old=bone.localRotation;
                    try
                    {
                        bone.localRotation=old*Quaternion.AngleAxis(15,Vector3.forward); body.BakeMesh(after);
                        Vector3[] moved=after.vertices; float max=0,feet=0;
                        for(int i=0;i<moved.Length;i++)
                        {
                            float delta=body.transform.TransformVector(moved[i]-original[i]).magnitude; max=Mathf.Max(max,delta);
                            if(body.transform.TransformPoint(original[i]).y<.07f) feet=Mathf.Max(feet,delta);
                        }
                        if(max<.002f) throw new Exception("Bone did not deform imported mesh: "+name);
                        if(name=="head" && feet>.001f) throw new Exception("Head pose moved the feet.");
                        result+=name+": maximum world-space displacement="+max.ToString("F6")+"m; foot displacement="+feet.ToString("F6")+"m\n";
                    }
                    finally {bone.localRotation=old;}
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(before); UnityEngine.Object.DestroyImmediate(after);}
            result+="Trial weights only: not a walk-cycle, foot-sliding, joint or fur-binding visual approval.\n";
            string output=Path.Combine(M2LookDevBuilder.Repository,"Artifacts/UnityEditor"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"m2-skin-test.txt"),result); Debug.Log("SORAJIRO M2 SKIN TEST: "+result);
        }

        private static void RequireScene()
        {
            if(SceneManager.GetActiveScene().path!=M2LookDevBuilder.ScenePath)
                throw new InvalidOperationException("Open the generated M2 lookdev scene first.");
        }

        [Serializable] private sealed class ShotEvidence
        {
            public string utc,shot,unity,gpu,api,aa;
            public Vector3 position,rotation;
            public float verticalFov;
            public bool dynamicResolution,dlss,fsr2,patchActive;
            public int width=1920,height=1080,warmupRenderRequests=24;
            public float fixedEV=8;
            public string scope="Actual HDRP StandardRequest camera output in Editor, not path tracing, Game-view screenshot, motion or performance benchmark";
            public string internalResolution="1920x1080 render target; dynamic scale false; camera dynamic resolution and DLSS/FSR2 disabled";
            public string assetState="generated_imported_lookdev_trial; back-head short-pile patch only; no walk animation";
            public bool ownerQualityApproved=false;
        }
    }
}
