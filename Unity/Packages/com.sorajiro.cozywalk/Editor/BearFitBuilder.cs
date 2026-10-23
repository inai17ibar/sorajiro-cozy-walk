using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class BearFitBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/BearFit_v002";
        public const string ScenePath=Output+"/L_LookDev.unity";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Art=>Path.Combine(Repo,"Artifacts/SourceArt/Bear/fit_v001");
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe=>"bear-fit-v002-"+GeneratedContentGuard.HashFile(Path.Combine(Art,"receipt.json"))+
            GeneratedContentGuard.HashFile(FleeceStudyBuilder.Output+"/generation-receipt.json")+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/BearFitBuilder.cs"))+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/BearAnimationBuilder.cs"))+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Runtime/BearFitPreview.cs"))+
            GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Runtime/PlushGait.cs"));
        public static void Preflight()
        {
            FleeceStudyBuilder.Preflight();FleeceStudyBuilder.VerifyAssets();
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked output refused");
            for(var d=new DirectoryInfo(Art);d!=null;d=d.Parent)
                if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked art refused");
            string receipt=File.ReadAllText(Path.Combine(Art,"receipt.json"));
            foreach(string name in new[]{"Sorajiro_Bear_Fit.blend","Sorajiro_Bear_Fit.fbx"})
                if(!receipt.Contains("\""+name+"\": \""+GeneratedContentGuard.HashFile(Path.Combine(Art,name))+"\""))throw new Exception("Art changed; preserve and use new revision");
        }
        [MenuItem("Sorajiro/Bear Fit/1. Create protected shape and lighting study")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            Directory.CreateDirectory(Output);File.Copy(Path.Combine(Art,"Sorajiro_Bear_Fit.fbx"),Output+"/Bear.fbx",false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Output+"/Bear.fbx");
            importer.isReadable=true;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.motionNodeName="root";
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;importer.weldVertices=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(string name in new[]{"BlueFleece","WhiteApplique","PileBlue","PileWhite","SafetyPlastic","Stitch"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(FleeceStudyBuilder.Output+"/"+name+"_Fleece.mat")??AssetDatabase.LoadAssetAtPath<Material>(TwilightLookDevBuilder.Output+"/"+name+".mat");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
            }
            importer.SaveAndReimport();
            var scene=EditorSceneManager.OpenScene(FleeceStudyBuilder.ScenePath);EditorSceneManager.SaveScene(scene,ScenePath);
            var previous=UnityEngine.Object.FindAnyObjectByType<FleeceVariantPreview>();previous.Apply(1);
            var motor=previous.GetComponent<SorajiroMotor>();var old=motor.visualRoot.gameObject;old.name="Baseline_Bear_Fleece2";
            var baselineAnimator=old.GetComponent<Animator>();
            UnityEngine.Object.DestroyImmediate(previous);UnityEngine.Object.DestroyImmediate(motor.GetComponent<BearAnimationDriver>());
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Bear.fbx"),motor.transform);
            model.name="Current_Bear_FullerBelly_WiderStance";model.transform.localPosition=old.transform.localPosition;model.transform.localRotation=old.transform.localRotation;
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;skin.lightProbeUsage=LightProbeUsage.BlendProbes;}
            BearAnimationBuilder.Attach(model,motor,Output);
            var baselineLights=new GameObject("Baseline_Lights");
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())light.transform.SetParent(baselineLights.transform,true);
            var currentLights=new GameObject("Current_Raking_Window_Lights");
            Light(currentLights,"Window_Side_Key",LightType.Rectangle,new Vector3(-.45f,.60f,-.30f),650,new Color(1,.96f,.90f),new Vector3(.45f,-.40f,.30f),18);
            Light(currentLights,"Window_Soft_Fill",LightType.Rectangle,new Vector3(.38f,.42f,-.42f),65,new Color(.94f,.97f,1),new Vector3(-.38f,-.12f,.42f),0);
            Light(currentLights,"Back_Soft_Rim",LightType.Rectangle,new Vector3(.35f,.52f,.12f),55,new Color(1,.92f,.82f),new Vector3(-.35f,-.22f,-.12f),0);
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="BearFit_IndirectFill";profile.Add<GradientSky>().exposure.Override(5.7f);var exposure=profile.Add<Exposure>();exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(6.3f);
            AssetDatabase.CreateAsset(profile,Output+"/FillVolume.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);EditorUtility.SetDirty(profile);
            var volume=new GameObject("Current_IndirectFill").AddComponent<Volume>();volume.transform.SetParent(currentLights.transform);volume.isGlobal=true;volume.priority=201;volume.sharedProfile=profile;
            var fit=motor.gameObject.AddComponent<BearFitPreview>();fit.baselineModel=old;fit.currentModel=model;fit.baselineAnimator=baselineAnimator;fit.currentAnimator=model.GetComponent<Animator>();fit.motor=motor;fit.driver=motor.GetComponent<BearAnimationDriver>();fit.baselineLights=baselineLights;fit.currentLights=currentLights;fit.Apply(2);
            motor.diagnosticLabel="素材は前回2：1 従来 / 2 光 / 3 光と体型";
            var camera=new GameObject("F05_FrontFullBody").AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,.21f,-.67f);camera.transform.LookAt(new Vector3(0,.21f,0));camera.fieldOfView=40;camera.nearClipPlane=.01f;camera.farClipPlane=25;
            var hd=camera.gameObject.AddComponent<HDAdditionalCameraData>();hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;hd.allowDynamicResolution=false;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.Refresh();GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();
        }
        public static void VerifyAssets(){if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Fit assets edited/missing");}
        public static void Check()
        {
            Preflight();VerifyAssets();if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open bear fit first");
            var fit=UnityEngine.Object.FindAnyObjectByType<BearFitPreview>();if(!fit || fit.variant!=2)throw new Exception("Expected current fit");
            var before=fit.baselineModel.GetComponentsInChildren<Transform>(true);var after=fit.currentModel.GetComponentsInChildren<Transform>(true);
            float Spread(Transform[] nodes)=>Mathf.Abs(nodes.Single(t=>t.name=="foot_L").position.x-nodes.Single(t=>t.name=="foot_R").position.x);
            if(Spread(after)-Spread(before)<.014f || Spread(after)-Spread(before)>.018f)throw new Exception("Imported stance mismatch");
            foreach(var skin in fit.currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.sharedMesh.bindposes.Length!=skin.bones.Length || skin.sharedMaterials.Any(m=>!m||!m.shader.isSupported))throw new Exception("Skin/material mismatch");
                if(skin.sharedMesh.vertices.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new Exception("Non-finite imported mesh");
            }
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/bear-fit-checks.txt"),"PASS: actual FBX and Generic rig loaded; stance +16mm; finite skin positions; supported materials; all source/output receipts intact.\nScene="+ScenePath+"\n");
        }
        [MenuItem("Sorajiro/Bear Fit/2. Open shape and lighting study")]
        public static void Open(){Preflight();VerifyAssets();EditorSceneManager.OpenScene(ScenePath);}
        private static void Light(GameObject parent,string name,LightType type,Vector3 position,float power,Color color,Vector3 forward,float angle)
        {
            var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(parent.transform);var hd=light.gameObject.AddComponent<HDAdditionalLightData>();light.type=type;light.lightUnit=type==LightType.Directional?LightUnit.Lux:LightUnit.Lumen;light.intensity=power;light.color=color;light.transform.position=position;light.transform.rotation=Quaternion.LookRotation(forward);light.shadows=angle>0?LightShadows.Soft:LightShadows.None;light.areaSize=new Vector2(.65f,.55f);light.range=5;hd.angularDiameter=angle;
        }
    }
}
