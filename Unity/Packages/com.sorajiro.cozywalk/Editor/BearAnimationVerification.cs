using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro.Editor
{
    public static class BearAnimationVerification
    {
        [MenuItem("Sorajiro/Twilight/6. Test editable animation clips (Edit)")]
        public static void ClipTests()
        {
            TwilightLookDevBuilder.Preflight();
            var scene=EditorSceneManager.NewPreviewScene();
            string result="Real FBX + editable native AnimationClip sampling in isolated Unity preview scene.\n";
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TwilightLookDevBuilder.Output+"/Bear.fbx"),scene);
                var animator=model.GetComponent<Animator>();
                if(!animator || !animator.avatar || !animator.avatar.isValid || animator.avatar.isHuman) throw new Exception("Expected valid Generic avatar.");
                var nodes=model.GetComponentsInChildren<Transform>(true);
                var positions=nodes.Select(t=>t.localPosition).ToArray(); var rotations=nodes.Select(t=>t.localRotation).ToArray(); var scales=nodes.Select(t=>t.localScale).ToArray();
                var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var before=skins.Select(s=>{var mesh=new Mesh(); s.BakeMesh(mesh); return mesh;}).ToArray();
                try
                {
                    foreach(string name in new[]{"Idle","Walk","Run","Jump","Fall","Land"})
                    {
                        for(int i=0;i<nodes.Length;i++) {nodes[i].localPosition=positions[i]; nodes[i].localRotation=rotations[i]; nodes[i].localScale=scales[i];}
                        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(TwilightLookDevBuilder.Output+"/"+name+".anim");
                        var curves=AnimationUtility.GetCurveBindings(clip);
                        if(clip.legacy || curves.Length!=56 || curves.Any(b=>!model.transform.Find(b.path))) throw new Exception("Unbound clip curves: "+name);
                        Vector3 origin=model.transform.position; clip.SampleAnimation(model,clip.length*.35f);
                        if(Vector3.Distance(origin,model.transform.position)>.00001f) throw new Exception("Clip changed physics root.");
                        result+=name+": duration="+clip.length+"s; 56 bound position/quaternion curves; loop="+AnimationUtility.GetAnimationClipSettings(clip).loopTime+"\n";
                        for(int s=0;s<skins.Length;s++)
                        {
                            var after=new Mesh(); skins[s].BakeMesh(after); var av=after.vertices; var bv=before[s].vertices; float maximum=0;
                            for(int i=0;i<av.Length;i++) maximum=Mathf.Max(maximum,skins[s].transform.TransformVector(av[i]-bv[i]).magnitude);
                            UnityEngine.Object.DestroyImmediate(after);
                            if(maximum<.0002f) throw new Exception("Clip did not deform skin: "+name+" / "+skins[s].name);
                            result+="  "+skins[s].name+": max world-space deformation="+maximum.ToString("F6")+"m\n";
                        }
                    }
                }
                finally {foreach(var mesh in before) UnityEngine.Object.DestroyImmediate(mesh);}
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
            Save("twilight-animation-clips.txt","PASS: "+result+"This is sampled deformation, not foot-contact, motion quality or a Player build approval.\n");
        }

        [MenuItem("Sorajiro/Twilight/7. Test live Animator transitions (Play, resets player)")]
        public static void RuntimeTests()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=TwilightLookDevBuilder.ScenePath) throw new Exception("Enter Play in Twilight first.");
            var driver=UnityEngine.Object.FindAnyObjectByType<BearAnimationDriver>(); var motor=driver.motor; var animator=driver.animator;
            if(motor.Paused || !motor.Focused) throw new Exception("Focused, unpaused player required.");
            var states=new HashSet<string>(); Quaternion rotation=motor.visualRoot.rotation;
            try
            {
                motor.ReturnToStart();
                for(int i=0;i<240;i++)
                {
                    float forward=i>=30 && i<55?1:0;
                    motor.Step(0,forward,i==90,false,1f/60); driver.Sync(1f/60); animator.Update(1f/60);
                    var info=animator.GetCurrentAnimatorStateInfo(0);
                    foreach(string name in new[]{"Locomotion","Jump","Fall","Land"}) if(info.IsName(name))states.Add(name);
                }
                if(states.Count!=4 || !motor.Grounded || animator.applyRootMotion || !animator.avatar.isValid) throw new Exception("Animator/physics transition test failed: "+string.Join(",",states));
                Save("twilight-animation-runtime.txt","PASS: production motor + animation driver + actual Generic Animator.Update; observed Locomotion, Jump, Fall, Land and grounded recovery; root motion off.\nSynchronous integration test, not keyboard, frame pacing or motion-quality approval.\n");
            }
            finally {motor.ReturnToStart(); motor.visualRoot.rotation=rotation; driver.Sync(1f/60); animator.Update(0);}
        }

        private static void Save(string name,string result)
        {string dir=Path.Combine(TwilightLookDevBuilder.Repository,"Artifacts/UnityEditor"); Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir,name),result); Debug.Log("SORAJIRO ANIMATION: "+result);}

        [MenuItem("Sorajiro/Twilight/9. Test planted sole motion (Edit)")]
        public static void SoleTests()
        {
            TwilightLookDevBuilder.Preflight();var scene=EditorSceneManager.NewPreviewScene();string result="";
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TwilightLookDevBuilder.Output+"/Bear.fbx"),scene);
                var body=model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Bear_SkinnedBody_ArtTrial");
                var restMesh=new Mesh();body.BakeMesh(restMesh);
                var rest=restMesh.vertices.Select(p=>body.transform.TransformPoint(p)).ToArray();
                float bottom=rest.Min(p=>p.y);var weights=body.sharedMesh.boneWeights;
                var nodes=model.GetComponentsInChildren<Transform>();var positions=nodes.Select(t=>t.localPosition).ToArray();var rotations=nodes.Select(t=>t.localRotation).ToArray();
                try
                {
                    foreach(string side in new[]{"L","R"})
                    {
                        int bone=Array.FindIndex(body.bones,t=>t.name=="foot_"+side);
                        var indices=Enumerable.Range(0,rest.Length).Where(i=>rest[i].y<bottom+.004f &&
                            ((weights[i].boneIndex0==bone && weights[i].weight0>.97f)||(weights[i].boneIndex1==bone && weights[i].weight1>.97f))).ToArray();
                        if(indices.Length<10)throw new Exception("No rigid sole sample vertices: "+side);
                        foreach(string name in new[]{"Walk","Run"})
                        {
                            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(TwilightLookDevBuilder.Output+"/"+name+".anim");
                            float speed=name=="Walk"?.28f:.52f;Vector3 first=Vector3.zero;float maximum=0;
                            for(int step=0;step<=12;step++)
                            {
                                for(int i=0;i<nodes.Length;i++){nodes[i].localPosition=positions[i];nodes[i].localRotation=rotations[i];}
                                float phase=.13f+.17f*step/12+(side=="R"?.5f:0),time=phase*clip.length;
                                clip.SampleAnimation(model,time);var mesh=new Mesh();body.BakeMesh(mesh);
                                var sampled=mesh.vertices;Vector3 center=Vector3.zero;foreach(int index in indices)center+=body.transform.TransformPoint(sampled[index]);center/=indices.Length;
                                center+=Vector3.forward*(time*speed);UnityEngine.Object.DestroyImmediate(mesh);
                                if(step==0)first=center;else maximum=Mathf.Max(maximum,Vector3.Distance(center,first));
                            }
                            if(maximum>.0015f)throw new Exception(name+" planted sole drifts "+maximum+"m: "+side);
                            result+=name+"/"+side+": "+indices.Length+" sole vertices; support centroid world drift="+maximum.ToString("F6")+"m over 13 real clip samples.\n";
                        }
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(restMesh);}
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            Save("twilight-sole-test.txt","PASS: imported weighted sole + real clips + controller-distance compensation.\n"+result+"Constant-speed straight support only; turns, transitions, obstacles and live foot contact still need visual review.\n");
        }
    }
}
