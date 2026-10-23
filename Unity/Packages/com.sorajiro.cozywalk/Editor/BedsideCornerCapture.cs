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
    public static class BedsideCornerCapture
    {
        private static readonly string[] Shots={"S05_Bedding","S02_Gameplay","S01_Portrait","S03_ThreeQuarter","S05_Bedding","S05_Bedding","S01_Portrait"};
        private static readonly string[] Variants={"Cotton_GI_ON","Cotton_GI_ON","Cotton_GI_ON","Cotton_GI_ON","Cotton_GI_OFF","Lit_GI_ON","Cotton_GI_OFF"};
        private static string Output,ScenePath;
        private static RenderTexture target;private static string folder;private static int shot,warmup;
        private static IndirectLightingController indirect;private static float multiplier,fixedEV;
        private static readonly Dictionary<Renderer,Material[]> materials=new Dictionary<Renderer,Material[]>();
        [MenuItem("Sorajiro/Bedside/3. Capture fabric and GI comparisons (Edit, 1080p)")]
        public static void Capture()
        {
            if(target)throw new Exception("Capture in progress");
            bool stable=SceneManager.GetActiveScene().path==BeddingStableBuilder.ScenePath;
            bool rounded=SceneManager.GetActiveScene().path==BeddingRoundBuilder.ScenePath;
            bool surface=SceneManager.GetActiveScene().path==BeddingSurfaceBuilder.ScenePath;
            bool drape=SceneManager.GetActiveScene().path==BeddingDrapeBuilder.ScenePath;
            bool contact=SceneManager.GetActiveScene().path==BeddingContactBuilder.ScenePath;
            bool refined=SceneManager.GetActiveScene().path==BeddingRefinementBuilder.ScenePath;
            if(stable)BeddingStableBuilder.Check();else if(rounded)BeddingRoundBuilder.Check();else if(surface)BeddingSurfaceBuilder.Check();else if(drape)BeddingDrapeBuilder.Check();else if(contact)BeddingContactBuilder.Check();else if(refined)BeddingRefinementBuilder.Check();else BedsideCornerBuilder.Check();
            Output=stable?BeddingStableBuilder.Output:rounded?BeddingRoundBuilder.Output:surface?BeddingSurfaceBuilder.Output:drape?BeddingDrapeBuilder.Output:contact?BeddingContactBuilder.Output:refined?BeddingRefinementBuilder.Output:BedsideCornerBuilder.Output;
            ScenePath=stable?BeddingStableBuilder.ScenePath:rounded?BeddingRoundBuilder.ScenePath:surface?BeddingSurfaceBuilder.ScenePath:drape?BeddingDrapeBuilder.ScenePath:contact?BeddingContactBuilder.ScenePath:refined?BeddingRefinementBuilder.ScenePath:BedsideCornerBuilder.ScenePath;
            folder=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/Visual/bedside",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            Directory.CreateDirectory(folder);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Output+"/LookDevVolume.asset");
            if(!profile.TryGet<IndirectLightingController>(out indirect))throw new Exception("Indirect volume missing");multiplier=indirect.indirectDiffuseLightingMultiplier.value;
            if(!profile.TryGet<Exposure>(out var exposure))throw new Exception("Fixed exposure missing");fixedEV=exposure.fixedExposure.value;
            materials.Clear();foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>())materials.Add(r,r.sharedMaterials);
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false};target.Create();
            shot=0;warmup=0;EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            Debug.Log("SORAJIRO BEDSIDE CAPTURE START: "+folder);
        }
        private static void Tick()
        {
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Capture interrupted");
                indirect.indirectDiffuseLightingMultiplier.value=Variants[shot].Contains("GI_OFF")?0:multiplier;
                foreach(var pair in materials)
                {
                    if(!pair.Key)throw new Exception("Renderer changed during capture");
                    pair.Key.sharedMaterials=Variants[shot]=="Lit_GI_ON"?pair.Value.Select(m=>
                    {
                        if(m && m.name.EndsWith("_CottonWool"))return AssetDatabase.LoadAssetAtPath<Material>(BedsideCornerBuilder.Source+"/"+m.name.Replace("_CottonWool","")+".mat");
                        return m;
                    }).ToArray():pair.Value;
                }
                var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.name==Shots[shot]);float aspect=camera.aspect;
                try
                {
                    camera.aspect=16f/9;var request=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("HDRP request unavailable");
                    RenderPipeline.SubmitRenderRequest(camera,request);if(++warmup<32)return;
                    string name=Shots[shot]+"_"+Variants[shot];var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                    var previous=RenderTexture.active;
                    try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());}
                    finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
                    var hd=camera.GetComponent<HDAdditionalCameraData>();
                    File.WriteAllText(Path.Combine(folder,name+".json"),JsonUtility.ToJson(new Frame{
                        utc=DateTime.UtcNow.ToString("O"),scene=ScenePath,unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                        api=SystemInfo.graphicsDeviceType.ToString(),shot=name,position=camera.transform.position,rotation=camera.transform.eulerAngles,
                        verticalFov=camera.fieldOfView,fixedEV=fixedEV,dynamicResolution=hd.allowDynamicResolution,lightmaps=LightmapSettings.lightmaps.Length,
                        apvBaked=AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(Output+"/APV_BakingSet.asset").HasBakedData(),
                        indirectMultiplier=indirect.indirectDiffuseLightingMultiplier.value},true));
                }
                finally{camera.aspect=aspect;}
                if(++shot==Shots.Length)
                {File.WriteAllText(Path.Combine(folder,"complete.txt"),"7 actual HDRP/Metal 1080p StandardRequest frames. Same cameras/exposure. GI_OFF disables indirect diffuse multiplier, not just one light source. Lit comparison uses v010 cloth shading with current baked lighting. No performance or visual approval.\n");Cleanup();Debug.Log("SORAJIRO BEDSIDE CAPTURE COMPLETE: "+folder);}
                else warmup=0;
            }
            catch(Exception e){Cleanup();Debug.LogException(e);}
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
            if(indirect)indirect.indirectDiffuseLightingMultiplier.value=multiplier;
            foreach(var pair in materials)if(pair.Key)pair.Key.sharedMaterials=pair.Value;materials.Clear();
            if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
        }
        [Serializable]private class Frame
        {
            public string utc,scene,unity,gpu,api,shot;public Vector3 position,rotation;public float verticalFov,indirectMultiplier,fixedEV;
            public int width=1920,height=1080,lightmaps;public bool dynamicResolution,apvBaked;
            public string scope="Actual Editor HDRP StandardRequest; TAA; fixed exposure recorded; no dynamic scale / DLSS / FSR; not a Player/performance/owner-approval claim";
        }
    }
}
