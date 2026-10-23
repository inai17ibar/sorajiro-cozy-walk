using System;
using System.Collections.Generic;
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
    public static class FleeceStudyBuilder
    {
        public const string Output="Assets/Sorajiro/Generated/Fleece_v005";
        public const string ScenePath=Output+"/L_LookDev.unity";
        public const string Source=TwilightLookDevBuilder.Output;
        public const string FabricPath=BedsideCornerBuilder.FabricPath;
        public const string HairPath="Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/ShaderGraph/Hair.shadergraph";
        private static string Repo=>TwilightLookDevBuilder.Repository;
        private static string Hdrp=>UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).version;
        private static string Recipe=>"fleece-v005-"+GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Editor/FleeceStudyBuilder.cs"))+GeneratedContentGuard.HashFile(Source+"/generation-receipt.json")+GeneratedContentGuard.HashFile(Path.Combine(Repo,"Unity/Packages/com.sorajiro.cozywalk/Runtime/FleeceVariantPreview.cs"))+GeneratedContentGuard.HashFile(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HDRenderPipeline).Assembly).resolvedPath+"/Runtime/RenderPipelineResources/ShaderGraph/Fabric CottonWool.shadergraph");
        public static void Preflight()
        {
            if(Application.unityVersion!="6000.6.5f1" || Hdrp!="17.6.0")throw new Exception("Reviewed Unity/HDRP required");
            if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning)throw new Exception("Stop Play/bake/compilation first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve owner scene edits first");
            var receipt=JsonUtility.FromJson<GeneratedReceipt>(File.ReadAllText(Source+"/generation-receipt.json"));
            GeneratedContentGuard.CanReuse(Source,Application.unityVersion,Hdrp,receipt.recipe);
            for(var d=new DirectoryInfo(Path.GetFullPath(Output));d!=null;d=d.Parent)
                if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked output refused");
        }
        [MenuItem("Sorajiro/Fleece/1. Create protected study")]
        public static void Create()
        {
            Preflight();if(GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))return;
            Directory.CreateDirectory(Output);
            File.Copy(Source+"/Bear.fbx",Output+"/Bear.fbx",false);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Output+"/Bear.fbx");
            importer.isReadable=true;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.motionNodeName="root";
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;importer.weldVertices=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(string n in new[]{"BlueFleece","WhiteApplique","SafetyPlastic","Stitch","PileBlue","PileWhite"})
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),n),AssetDatabase.LoadAssetAtPath<Material>(Source+"/"+n+".mat"));
            importer.SaveAndReimport();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var player=new GameObject("Fleece_Master_Bear_Trial");player.layer=2;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Bear.fbx"),player.transform);
            visual.name="MasterBear_EDITABLE_GENERIC_RIG";foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
            var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            var pile=skins.Single(r=>r.name.Contains("Pile"));var body=skins.Single(r=>r!=pile);
            float min=skins.SelectMany(r=>r.sharedMesh.vertices.Select(p=>r.transform.TransformPoint(p).y)).Min();
            visual.transform.localPosition=Vector3.up*(-min-.005f);visual.transform.localRotation=Quaternion.Euler(0,180,0);
            player.transform.position=Vector3.up*.01f;
            var normalAndColor=FleeceTextures();
            var fabrics=new Dictionary<string,Material>();
            foreach(string name in new[]{"BlueFleece","WhiteApplique","PileBlue","PileWhite"})
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>(Source+"/"+name+".mat");
                var mat=new Material(AssetDatabase.LoadAssetAtPath<Shader>(FabricPath)){name=name+"_Fleece"};
                mat.SetColor("_BaseColor",source.GetColor("_BaseColor")*(name.StartsWith("Pile")?1.06f:1));mat.SetTexture("_BaseColorMap",normalAndColor.color);
                mat.SetTexture("_NormalMap",normalAndColor.normal);mat.SetFloat("_NormalMapStrength",1.1f);
                mat.SetVector("_uvBaseST",new Vector4(4,4,0,0));mat.SetFloat("_SmoothnessMin",.08f);mat.SetFloat("_SmoothnessMax",.28f);
                mat.SetColor("_SpecularColor",new Color(.045f,.045f,.045f));mat.SetFloat("_SubsurfaceMask",0);mat.SetFloat("_TransmissionEnable",0);
                mat.SetFloat("_useThreadMap",0);mat.SetFloat("_ThreadNormalStrength",0);mat.SetFloat("_ThreadAOStrength01",0);mat.SetFloat("_ThreadSmoothnessScale",0);
                mat.SetFloat("_DoubleSidedEnable",1);mat.SetFloat("_DoubleSidedNormalMode",2);
                mat.SetTexture("_FuzzMap",normalAndColor.color);mat.SetFloat("_FuzzStrength",.28f);mat.SetFloat("_FuzzMapUVScale",6);
                HDMaterial.ValidateMaterial(mat);AssetDatabase.CreateAsset(mat,Output+"/"+mat.name+".mat");fabrics.Add(name,mat);
            }
            var groom=Groom(pile.sharedMesh);
            AssetDatabase.CreateAsset(groom.mesh,Output+"/GroomedShortPile.asset");
            var preview=player.AddComponent<FleeceVariantPreview>();preview.body=body;preview.pile=pile;
            preview.baselinePile=pile.sharedMesh;preview.groomedPile=groom.mesh;
            preview.baselineBodyMaterials=body.sharedMaterials;preview.baselinePileMaterials=pile.sharedMaterials;
            preview.fleeceBodyMaterials=body.sharedMaterials.Select(m=>fabrics.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
            preview.fleecePileMaterials=pile.sharedMaterials.Select(m=>fabrics[m.name]).ToArray();preview.Apply(2);
            foreach(var r in skins){r.updateWhenOffscreen=true;r.shadowCastingMode=ShadowCastingMode.On;r.lightProbeUsage=LightProbeUsage.BlendProbes;}
            var cc=player.AddComponent<CharacterController>();cc.height=.4f;cc.radius=.085f;cc.center=Vector3.up*.2f;cc.stepOffset=.04f;cc.skinWidth=.005f;
            var motor=player.AddComponent<SorajiroMotor>();motor.visualRoot=visual.transform;motor.walkSpeed=.28f;motor.runSpeed=.52f;
            motor.diagnosticLabel="短毛の画質試作：1 基準 / 2 布素材 / 3 毛束";
            var animator=visual.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Source+"/BearMotion.controller");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=player.AddComponent<BearAnimationDriver>();driver.motor=motor;driver.animator=animator;
            var groundMat=new Material(Shader.Find("HDRP/Lit")){name="NeutralWarmFloor"};groundMat.SetColor("_BaseColor",new Color(.43f,.39f,.34f));groundMat.SetFloat("_Smoothness",.16f);HDMaterial.ValidateMaterial(groundMat);AssetDatabase.CreateAsset(groundMat,Output+"/Floor.mat");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Studio_Floor";ground.transform.position=new Vector3(0,-.045f,0);ground.transform.localScale=new Vector3(5,.10f,5);ground.GetComponent<Renderer>().sharedMaterial=groundMat;
            var game=Camera("GameplayCamera",new Vector3(0,.35f,-1.1f),new Vector3(0,.23f,0),40);game.enabled=true;game.tag="MainCamera";
            var orbit=game.gameObject.AddComponent<OrbitCamera>();orbit.target=motor;orbit.pitch=8;orbit.distance=.85f;motor.view=game.transform;
            Camera("F01_Portrait",new Vector3(0,.31f,-.60f),new Vector3(0,.286f,0),36);
            Camera("F02_FullBody",new Vector3(.28f,.28f,-.68f),new Vector3(0,.21f,0),38);
            Camera("F03_Surface",new Vector3(.035f,.31f,-.23f),new Vector3(.02f,.30f,0),30);
            var patch=Patch(fabrics["PileBlue"]);patch.SetActive(false);
            Camera("F04_Patch",new Vector3(.25f,.235f,-.16f),new Vector3(.25f,.235f,0),35);
            Light("Studio_Key",LightType.Directional,new Vector3(-1,2,-2),400,new Color(1,.96f,.90f),new Vector3(.3f,-.5f,.8f));
            Light("Studio_Window_Soft",LightType.Rectangle,new Vector3(-.4f,.65f,-.40f),220,new Color(.94f,.97f,1),new Vector3(.4f,-.4f,.4f));
            Light("Studio_Rim",LightType.Rectangle,new Vector3(.4f,.48f,.10f),70,new Color(1,.88f,.76f),new Vector3(-.4f,-.2f,-.1f));
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Fleece_Fixed_Studio";
            profile.Add<Exposure>().mode.Override(ExposureMode.Fixed);profile.components.OfType<Exposure>().Single().fixedExposure.Override(7);
            var sky=profile.Add<GradientSky>();sky.top.Override(new Color(.42f,.48f,.55f));sky.middle.Override(new Color(.50f,.49f,.47f));sky.bottom.Override(new Color(.26f,.23f,.20f));sky.exposure.Override(6);
            profile.Add<VolumetricClouds>().enable.Override(false);profile.Add<CloudLayer>().opacity.Override(0);
            var env=profile.Add<VisualEnvironment>();env.skyType.Override(SkySettings.GetUniqueID<GradientSky>());env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            profile.Add<DepthOfField>().focusMode.Override(DepthOfFieldMode.Off);profile.Add<MotionBlur>().intensity.Override(0);profile.Add<Bloom>().intensity.Override(0);
            var ao=profile.Add<ScreenSpaceAmbientOcclusion>();ao.intensity.Override(.45f);ao.radius.Override(.04f);
            AssetDatabase.CreateAsset(profile,Output+"/LookDevVolume.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);EditorUtility.SetDirty(profile);
            var volume=new GameObject("Fleece_Studio_Volume").AddComponent<Volume>();volume.isGlobal=true;volume.priority=200;volume.sharedProfile=profile;
            var observations=new Observation{originalFibers=groom.roots,currentFibers=groom.roots*2,vertices=groom.mesh.vertexCount,triangles=(int)groom.mesh.GetIndexCount(0)/3+(int)groom.mesh.GetIndexCount(1)/3};
            File.WriteAllText(Output+"/fiber-observations.json",JsonUtility.ToJson(observations,true));
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.Refresh();
            GeneratedContentGuard.WriteReceipt(Output,Application.unityVersion,Hdrp,Recipe);AssetDatabase.Refresh();
            Debug.Log("SORAJIRO FLEECE STUDY GENERATED: "+JsonUtility.ToJson(observations));
        }
        public static void VerifyAssets(){if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Fleece assets missing or edited");}
        public static void Check()
        {
            Preflight();if(!GeneratedContentGuard.CanReuse(Output,Application.unityVersion,Hdrp,Recipe))throw new Exception("Create study first");
            if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open fleece study first");
            var p=UnityEngine.Object.FindAnyObjectByType<FleeceVariantPreview>();
            if(!p || p.body.bones.Length!=p.pile.bones.Length || p.groomedPile.bindposes.Length!=p.baselinePile.bindposes.Length)throw new Exception("Rig binding mismatch");
            if(!p.groomedPile || p.groomedPile.vertexCount<=p.baselinePile.vertexCount || p.fleeceBodyMaterials.Any(m=>!m || !m.shader.isSupported))throw new Exception("Fleece assets/shader unavailable");
            var log="PASS: original model preserved; groomed pile mesh, shared bone binding and HDRP fabric materials loaded. Real visual/motion/performance approval remains separate.\nScene="+ScenePath+"\n";
            File.WriteAllText(Path.Combine(Repo,"Artifacts/UnityEditor/fleece-checks.txt"),log);
        }
        public static void Open(){Preflight();EditorSceneManager.OpenScene(ScenePath);}
        private static (Texture2D normal,Texture2D color) FleeceTextures()
        {
            const int size=1024;var h=new float[size*size];var random=new System.Random(4712);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)h[y*size+x]=.25f*Mathf.PerlinNoise(x*.045f,y*.045f);
            for(int fiber=0;fiber<42000;fiber++)
            {
                float x=(float)random.NextDouble()*size,y=(float)random.NextDouble()*size,length=5+(float)random.NextDouble()*14;
                float a=Mathf.PerlinNoise(x*.008f,y*.008f)*Mathf.PI*4+(float)random.NextDouble()*.8f;
                for(int j=0;j<20;j++)
                {
                    float t=j/19f,px=x+Mathf.Cos(a)*length*t+Mathf.Sin(a)*2*t*t,py=y+Mathf.Sin(a)*length*t-Mathf.Cos(a)*2*t*t;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {int ix=Mathf.FloorToInt(px)+dx,iy=Mathf.FloorToInt(py)+dy;float d=(ix-px)*(ix-px)+(iy-py)*(iy-py);h[((iy%size+size)%size)*size+(ix%size+size)%size]+=.075f*Mathf.Exp(-d*2)*Mathf.Sin(Mathf.PI*t);}
                }
            }
            var normals=new Color[size*size];var color=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int i=y*size+x;float dx=(h[y*size+(x+1)%size]-h[y*size+(x+size-1)%size])*3.0f,dy=(h[((y+1)%size)*size+x]-h[((y+size-1)%size)*size+x])*3.0f;
                Vector3 n=new Vector3(-dx,-dy,1).normalized;normals[i]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
                float value=Mathf.Clamp(.76f+h[i]*.50f,.70f,1);color[i]=new Color(value,value,value,1);
            }
            return (Texture("Fleece_Filaments_Normal",normals,size,true),Texture("Fleece_Microclump_Color",color,size,false));
        }
        private static Texture2D Texture(string name,Color[] colors,int size,bool normal)
        {
            var t=new Texture2D(size,size,TextureFormat.RGBA32,false,true);t.SetPixels(colors);t.Apply();string path=Output+"/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.sRGBTexture=false;i.mipmapEnabled=true;i.wrapMode=TextureWrapMode.Repeat;i.anisoLevel=8;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static (Mesh mesh,int roots) Groom(Mesh original)
        {
            var positions=original.vertices;var normals=original.normals;var uv=original.uv;var weights=original.boneWeights;
            var parent=Enumerable.Range(0,positions.Length).ToArray();
            Func<int,int> find=null;find=x=>{while(parent[x]!=x){parent[x]=parent[parent[x]];x=parent[x];}return x;};
            Action<int,int> union=(a,b)=>{a=find(a);b=find(b);if(a!=b)parent[b]=a;};
            var material=new int[positions.Length];
            for(int m=0;m<original.subMeshCount;m++){var tris=original.GetTriangles(m);for(int j=0;j<tris.Length;j+=3){union(tris[j],tris[j+1]);union(tris[j],tris[j+2]);material[tris[j]]=material[tris[j+1]]=material[tris[j+2]]=m;}}
            // FBX can split corners for tangents/UVs. Reconnect exact coincident positions
            // within each material before identifying the original two-quad fibers.
            var coincident=new Dictionary<(Vector3,int),int>();var canonical=new bool[positions.Length];
            for(int i=0;i<positions.Length;i++)
            {var key=(positions[i],material[i]);if(coincident.TryGetValue(key,out int first))union(i,first);else{coincident.Add(key,i);canonical[i]=true;}}
            var groups=new Dictionary<int,List<int>>();for(int i=0;i<positions.Length;i++){if(!canonical[i])continue;int root=find(i);if(!groups.TryGetValue(root,out var g))groups[root]=g=new List<int>(6);g.Add(i);}
            if(groups.Count<100000 || groups.Count>200000 || groups.Values.Any(g=>g.Count!=6))throw new Exception("Unexpected source ribbon topology; preserve partial revision. Components="+groups.Count);
            var vertices=new List<Vector3>(groups.Count*12);var ns=new List<Vector3>(groups.Count*12);var uvs=new List<Vector2>(groups.Count*12);var ws=new List<BoneWeight>(groups.Count*12);var tangents=new List<Vector4>(groups.Count*12);var indices=new[]{new List<int>(),new List<int>()};var rng=new System.Random(4721);
            foreach(var g in groups.Values)
            {
                var n=normals[g[0]].normalized;float min=g.Min(i=>Vector3.Dot(positions[i],n));var roots=g.Where(i=>Vector3.Dot(positions[i],n)<min+.00001f).ToArray();
                Vector3 p=Vector3.zero;Vector2 tex=Vector2.zero;foreach(int i in roots){p+=positions[i];tex+=uv[i];}p/=roots.Length;tex/=roots.Length;
                Vector3 down=new Vector3(.25f*Mathf.Sin(p.y*137+p.z*71),-1,.20f*Mathf.Cos(p.x*149));var tangent=Vector3.ProjectOnPlane(down,n).normalized;if(tangent.sqrMagnitude<.1f)tangent=Vector3.Cross(n,Vector3.right).normalized;var side=Vector3.Cross(n,tangent).normalized;
                for(int strand=0;strand<2;strand++)
                {
                    float rise=.00045f+(float)rng.NextDouble()*.00055f,travel=.0010f+(float)rng.NextDouble()*.0010f,width=.000025f+(float)rng.NextDouble()*.000030f,curl=((float)rng.NextDouble()-.5f)*.0007f;
                    Vector3 root=p-n*.00003f+side*((float)rng.NextDouble()-.5f)*.0003f;
                    int start=vertices.Count;
                    for(int j=0;j<3;j++)
                    {
                        float t=j*.5f;var center=root+n*(rise*Mathf.Sin(t*Mathf.PI*.60f))+tangent*(travel*t)+side*(curl*t*t);float w=width*(1-t*.90f);
                        vertices.Add(center-side*w);vertices.Add(center+side*w);ns.Add((n-side*.85f).normalized);ns.Add((n+side*.85f).normalized);for(int edge=0;edge<2;edge++){var tangentNormal=Vector3.ProjectOnPlane(side,ns[ns.Count-2+edge]).normalized;tangents.Add(new Vector4(tangentNormal.x,tangentNormal.y,tangentNormal.z,1));}uvs.Add(tex);uvs.Add(tex);ws.Add(weights[roots[0]]);ws.Add(weights[roots[0]]);
                    }
                    var ind=indices[material[g[0]]];ind.AddRange(new[]{start,start+3,start+1,start,start+2,start+3,start+2,start+5,start+3,start+2,start+4,start+5});
                }
            }
            var mesh=new Mesh{name="Short_Laid_Fleece_2x",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(ns);mesh.SetTangents(tangents);mesh.SetUVs(0,uvs);mesh.boneWeights=ws.ToArray();mesh.bindposes=original.bindposes;mesh.subMeshCount=2;for(int i=0;i<2;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateBounds();return(mesh,groups.Count);
        }
        private static GameObject Patch(Material fabric)
        {
            var root=new GameObject("Fleece_Patch_Comparison");
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Patch_Base";sphere.transform.SetParent(root.transform);sphere.transform.position=new Vector3(.25f,.235f,0);sphere.transform.localScale=Vector3.one*.07f;sphere.GetComponent<Renderer>().sharedMaterial=fabric;UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var random=new System.Random(77);var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int k=0;k<14000;k++)
            {
                float x=((float)random.NextDouble()-.5f)*.05f,y=((float)random.NextDouble()-.5f)*.05f;if(x*x+y*y>=.035f*.035f)continue;float z=-Mathf.Sqrt(.035f*.035f-x*x-y*y);var n=new Vector3(x,y,z).normalized;var t=Vector3.ProjectOnPlane(Vector3.down,n).normalized;var side=Vector3.Cross(n,t).normalized;int start=vertices.Count;
                for(int j=0;j<3;j++){float f=j*.5f;var p=new Vector3(.25f+x,.235f+y,z)+n*(.0008f*f)+t*(.0014f*f);float w=.00005f*(1-f*.9f);vertices.Add(p-side*w);vertices.Add(p+side*w);normals.Add(n);normals.Add(n);uv.Add(new Vector2(0,1-f));uv.Add(new Vector2(1,1-f));}
                triangles.AddRange(new[]{start,start+3,start+1,start,start+2,start+3,start+2,start+5,start+3,start+2,start+4,start+5});
            }
            var mesh=new Mesh{name="Small_Aligned_Ribbon_Patch",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();if(vertices.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new Exception("Non-finite patch rejected");AssetDatabase.CreateAsset(mesh,Output+"/Patch.asset");
            var fibers=new GameObject("Patch_Fibers");fibers.transform.SetParent(root.transform);fibers.AddComponent<MeshFilter>().sharedMesh=mesh;fibers.AddComponent<MeshRenderer>().sharedMaterial=fabric;
            var hair=new Material(AssetDatabase.LoadAssetAtPath<Shader>(HairPath)){name="Patch_Hair_Shaded_Ribbons"};hair.SetColor("_BaseColor",new Color(.29f,.63f,.82f));hair.SetFloat("_SurfaceType",0);hair.SetFloat("_DoubleSidedEnable",1);hair.SetFloat("_DoubleSidedNormalMode",2);hair.SetFloat("_SmoothnessMin",.15f);hair.SetFloat("_SmoothnessMax",.3f);hair.SetFloat("_Specular",.3f);hair.SetFloat("_SecondarySpecular",.1f);hair.SetFloat("_TransmissionRim",.2f);hair.SetColor("_TransmissionColor",new Color(.2f,.3f,.4f));HDMaterial.ValidateMaterial(hair);AssetDatabase.CreateAsset(hair,Output+"/Patch_Hair.mat");return root;
        }
        private static Camera Camera(string name,Vector3 position,Vector3 target,float fov)
        {
            var c=new GameObject(name).AddComponent<Camera>();c.enabled=false;c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=fov;c.nearClipPlane=.01f;c.farClipPlane=25;var data=c.gameObject.AddComponent<HDAdditionalCameraData>();data.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;data.allowDynamicResolution=false;return c;
        }
        private static void Light(string name,LightType type,Vector3 position,float intensity,Color color,Vector3 forward)
        {
            var l=new GameObject(name).AddComponent<Light>();var hd=l.gameObject.AddComponent<HDAdditionalLightData>();l.type=type;l.lightUnit=type==LightType.Directional?LightUnit.Lux:LightUnit.Lumen;l.intensity=intensity;l.color=color;l.transform.position=position;l.transform.rotation=Quaternion.LookRotation(forward);l.shadows=type==LightType.Directional?LightShadows.Soft:LightShadows.None;l.areaSize=Vector2.one*.6f;hd.angularDiameter=8;l.range=5;
        }
        [Serializable]private class Observation{public int originalFibers,currentFibers,vertices,triangles;public string state="generated_mesh_fleece_trial_not_approved",scope="Existing body, applique and rig untouched. Two narrower laid curved ribbons per original root; no strand simulator. Not an FPS claim.";}
    }
}
