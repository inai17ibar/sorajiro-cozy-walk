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
    public static class TwilightVerification
    {
        private static readonly string[] Shots={"S01_Portrait","S02_Gameplay","S03_ThreeQuarter","S04_Contact","S05_Bedding","S06_Back","S02_Gameplay","S01_Portrait"};
        private static string folder;
        private static int shot, warmup;
        private static RenderTexture target;
        private static GameObject patch;
        private static bool oldPatchActive;

        [MenuItem("Sorajiro/Twilight/4. Capture fixed shots and full-pile comparison (Edit, 1080p)")]
        public static void Capture()
        {
            TwilightLookDevBuilder.Preflight(); RequireScene();
            if(target) throw new InvalidOperationException("Capture already running.");
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/twilight",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            if(Directory.Exists(folder)) throw new IOException("Capture revision already exists.");
            Directory.CreateDirectory(folder);
            patch=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Single(t=>t.name=="FullBody_ShortPile_TRIAL").gameObject;
            oldPatchActive=patch.activeSelf; patch.SetActive(true);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){name="SorajiroFixed1080p",useDynamicScale=false};
            target.Create(); shot=0; warmup=0;
            EditorApplication.update+=Tick;
            AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            Debug.Log("SORAJIRO TWILIGHT CAPTURE started: "+folder+"; actual HDRP StandardRequest; not offline/path-traced or performance evidence.");
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
                    string id=Shots[shot]+(patch.activeSelf?"_pile_ON":"_pile_OFF");
                    var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false,false);
                    RenderTexture previous=RenderTexture.active;
                    try {RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,id+".png"),texture.EncodeToPNG());}
                    finally {RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(texture);}
                    var hd=camera.GetComponent<HDAdditionalCameraData>();
                    var data=new ShotEvidence{utc=DateTime.UtcNow.ToString("O"),shot=id,unity=Application.unityVersion,scene=SceneManager.GetActiveScene().path,
                        gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),
                        position=camera.transform.position,rotation=camera.transform.eulerAngles,verticalFov=camera.fieldOfView,
                        dynamicResolution=hd.allowDynamicResolution,aa=hd.antialiasing.ToString(),
                        dlss=hd.allowDeepLearningSuperSampling,fsr2=hd.allowFidelityFX2SuperResolution,patchActive=patch.activeSelf};
                    File.WriteAllText(Path.Combine(folder,id+".json"),JsonUtility.ToJson(data,true)+"\n");
                }
                finally {camera.aspect=previousAspect;}
                if(++shot==Shots.Length)
                {
                    File.WriteAllText(Path.Combine(folder,"complete.txt"),"8 actual HDRP request frames saved. Verify PNG dimensions and pixels. No motion/performance/quality approval.\n");
                    Debug.Log("SORAJIRO TWILIGHT CAPTURE COMPLETE: "+folder); Cleanup();
                }
                else {warmup=0; patch.SetActive(shot<6);}
            }
            catch(Exception e){Debug.LogException(e); Cleanup();}
        }

        private static void Cleanup()
        {
            EditorApplication.update-=Tick; AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(patch) patch.SetActive(oldPatchActive);
            if(target){target.Release(); UnityEngine.Object.DestroyImmediate(target); target=null;}
        }

        [MenuItem("Sorajiro/Twilight/5. Test imported body and full-pile skin (Edit)")]
        public static void SkinTest()
        {
            TwilightLookDevBuilder.Preflight(); RequireScene();
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(TwilightLookDevBuilder.Output+"/LookDevVolume.asset");
            if(profile.components.Count!=14 || profile.components.Any(c=>!c || !EditorUtility.IsPersistent(c)))
                throw new Exception("Volume components were not saved as real subassets.");
            var volume=UnityEngine.Object.FindObjectsByType<Volume>().Single(v=>v.sharedProfile==profile);
            VolumeManager.instance.Update(Camera.main.transform,1);
            var stack=VolumeManager.instance.stack;
            if(stack.GetComponent<Exposure>().fixedExposure.value!=6.5f || stack.GetComponent<VolumetricClouds>().enable.value)
                throw new Exception("Effective lookdev Volume settings incorrect.");
            var body=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>()
                .Single(r=>r.name=="Bear_SkinnedBody_ArtTrial");
            if(body.sharedMesh.uv.Length!=body.sharedMesh.vertexCount) throw new Exception("Missing imported UVs.");
            if(body.bones.Length<7) throw new Exception("Missing full-body skin bones.");
            if(body.sharedMaterials.Any(m=>!m || m.shader.name!="HDRP/Lit")) throw new Exception("Incorrect HDRP material remap.");
            var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>();
            string details="";
            foreach(var skin in skins)
            {
                if(skin.bones.Length<7 || skin.sharedMesh.uv.Length!=skin.sharedMesh.vertexCount) throw new Exception("Missing full-body skin or UV: "+skin.name);
                details+=skin.name+": vertices="+skin.sharedMesh.vertexCount+"; triangles="+(skin.sharedMesh.triangles.Length/3)+"; bones="+skin.bones.Length+"\n";
            }
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
            result+=details;
            var fur=skins.Single(r=>r.name=="FullBody_ShortPile_TRIAL");
            foreach(string name in new[]{"head","arm_L","leg_L"})
            {
                Transform bone=fur.bones.Single(b=>b.name==name); Quaternion old=bone.localRotation; var a=new Mesh(); var b=new Mesh();
                try
                {
                    fur.BakeMesh(a); bone.localRotation=old*Quaternion.AngleAxis(15,Vector3.forward); fur.BakeMesh(b);
                    Vector3[] av=a.vertices,bv=b.vertices; float max=0;
                    for(int i=0;i<av.Length;i++) max=Mathf.Max(max,fur.transform.TransformVector(av[i]-bv[i]).magnitude);
                    if(max<.002f) throw new Exception("Full-body fur did not deform: "+name);
                    result+="full pile "+name+": max displacement="+max.ToString("F6")+"m\n";
                }
                finally{bone.localRotation=old; UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);}
            }
            result+="Trial weights only: clip playback, foot-sliding, joints and fur-binding still require motion inspection.\n";
            string output=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"twilight-skin-test.txt"),result); Debug.Log("SORAJIRO TWILIGHT SKIN TEST: "+result);
        }

        private static void RequireScene()
        {
            if(SceneManager.GetActiveScene().path!=TwilightLookDevBuilder.ScenePath)
                throw new InvalidOperationException("Open the generated Twilight lookdev scene first.");
        }

        [MenuItem("Sorajiro/Twilight/10. Test room surfaces and reflection capture (Edit)")]
        public static void SurfaceTest()
        {
            TwilightLookDevBuilder.Preflight();RequireScene();
            var textile=AssetDatabase.LoadAssetAtPath<Material>(TwilightLookDevBuilder.Output+"/BlueTextile.mat");
            var texture=new Texture2D(2,2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(TwilightLookDevBuilder.Output+"/CloudQuilt_original.png"));
                Color cloud=texture.GetPixel(256,256),blue=texture.GetPixel(32,32);
                if(cloud.r-blue.r<.25f || blue.b-blue.r<.15f)throw new Exception("Cloud quilt lost blue/white contrast.");
                if(AssetDatabase.GetAssetPath(textile.GetTexture("_BaseColorMap"))!=TwilightLookDevBuilder.Output+"/CloudQuilt_original.png")throw new Exception("Quilt map not bound to imported material.");
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            var probe=UnityEngine.Object.FindAnyObjectByType<HDAdditionalReflectionData>();
            if(!probe || !probe.texture || probe.texture.width<64)throw new Exception("Actual reflection texture unavailable; capture fixed frames first.");
            var oak=AssetDatabase.LoadAssetAtPath<Material>(TwilightLookDevBuilder.Output+"/OakLight.mat");
            var mask=oak.GetTexture("_MaskMap");
            if(!mask || ((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mask))).sRGBTexture)
                throw new Exception("HDRP wood mask missing or incorrectly imported as sRGB.");
            var filters=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
            string detail="";
            foreach(string name in new[]{"Rattan","KnitIce","DistantPine"})
            {
                var filter=filters.Single(f=>f.name=="Room_"+name);
                if(!filter.sharedMesh || filter.sharedMesh.vertexCount<100 || filter.sharedMesh.uv.Length!=filter.sharedMesh.vertexCount)
                    throw new Exception("Detailed prop lost mesh or UVs: "+name);
                if(filter.GetComponent<Renderer>().sharedMaterials.All(m=>!m || m.name!=name))throw new Exception("Missing remap: "+name);
                detail+=name+" imported vertices="+filter.sharedMesh.vertexCount+"; ";
            }
            foreach(string name in new[]{"KnitIce","KnitShade","RugBlue"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(TwilightLookDevBuilder.Output+"/"+name+".mat");
                if(mat.GetFloat("_MaterialID")!=4 || mat.GetColor("_SpecularColor").maxColorComponent>.01f)
                    throw new Exception("Textile specular workflow lost: "+name);
            }
            string result="PASS: actual generated quilt PNG blue/white contrast, imported material binding, linear HDRP wood mask, low-specular textiles, HDRP reflection texture="+probe.texture.width+"px.\n"+detail+"\nScene="+TwilightLookDevBuilder.ScenePath+"\nReflection capture exists; reflection quality and GPU workload are not benchmarked.\n";
            File.WriteAllText(Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor/twilight-surfaces-test.txt"),result);Debug.Log("SORAJIRO SURFACES: "+result);
        }

        [Serializable] private sealed class ShotEvidence
        {
            public string utc,shot,unity,gpu,api,aa,scene;
            public Vector3 position,rotation;
            public float verticalFov;
            public bool dynamicResolution,dlss,fsr2,patchActive;
            public int width=1920,height=1080,warmupRenderRequests=24;
            public float fixedEV=6.5f;
            public string scope="Actual HDRP StandardRequest camera output in Editor, not path tracing, Game-view screenshot, motion or performance benchmark";
            public string internalResolution="1920x1080 render target; dynamic scale false; camera dynamic resolution and DLSS/FSR2 disabled";
            public string assetState="master_bear_full_body_pile_rig_animation_and_twilight_room_trial";
            public bool ownerQualityApproved=false;
        }
    }
}
