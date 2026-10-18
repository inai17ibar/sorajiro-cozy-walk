using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sorajiro.Editor
{
    public static class BearAnimationBuilder
    {
        public static void Attach(GameObject visual,SorajiroMotor motor,string output)
        {
            var controller=AnimatorController.CreateAnimatorControllerAtPath(output+"/BearMotion.controller");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("CycleRate",AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
            var machine=controller.layers[0].stateMachine;
            var blend=new BlendTree{name="PlushLocomotion",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(blend,controller);
            blend.AddChild(Clip(visual,output,"Idle",2,true),0);
            blend.AddChild(Clip(visual,output,"Walk",.52f,true),motor.walkSpeed);
            blend.AddChild(Clip(visual,output,"Run",.40f,true),motor.runSpeed);
            var locomotion=machine.AddState("Locomotion"); locomotion.motion=blend; machine.defaultState=locomotion;
            locomotion.speedParameter="CycleRate"; locomotion.speedParameterActive=true;
            var jump=machine.AddState("Jump"); jump.motion=Clip(visual,output,"Jump",.20f,false);
            var fall=machine.AddState("Fall"); fall.motion=Clip(visual,output,"Fall",.35f,true);
            var land=machine.AddState("Land"); land.motion=Clip(visual,output,"Land",.16f,false);
            var takeoff=Transition(locomotion,jump,.045f); takeoff.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded"); takeoff.AddCondition(AnimatorConditionMode.Greater,.02f,"VerticalSpeed");
            var drop=Transition(locomotion,fall,.07f); drop.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded"); drop.AddCondition(AnimatorConditionMode.Less,.02f,"VerticalSpeed");
            var apex=Transition(jump,fall,.055f); apex.AddCondition(AnimatorConditionMode.Less,.02f,"VerticalSpeed");
            foreach(var air in new[]{jump,fall}) {var t=Transition(air,land,.025f); t.AddCondition(AnimatorConditionMode.If,0,"Grounded");}
            var recover=Transition(land,locomotion,.04f); recover.hasExitTime=true; recover.exitTime=.9f;
            var bounce=Transition(land,jump,.025f); bounce.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded"); bounce.AddCondition(AnimatorConditionMode.Greater,.02f,"VerticalSpeed");
            var animator=visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=motor.gameObject.AddComponent<BearAnimationDriver>(); driver.motor=motor; driver.animator=animator;
        }

        private static AnimatorStateTransition Transition(AnimatorState a,AnimatorState b,float seconds)
        {var t=a.AddTransition(b); t.hasExitTime=false; t.hasFixedDuration=true; t.duration=seconds; t.interruptionSource=TransitionInterruptionSource.SourceThenDestination; return t;}

        private static AnimationClip Clip(GameObject root,string output,string name,float duration,bool loop)
        {
            var clip=new AnimationClip{name=name,frameRate=60};
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=loop; settings.loopBlend=loop; AnimationUtility.SetAnimationClipSettings(clip,settings);
            string[] names={"body","head","arm_L","arm_R","leg_L","leg_R","foot_L","foot_R"};
            var bones=names.ToDictionary(n=>n,n=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n));
            var rest=names.ToDictionary(n=>n,n=>root.transform.InverseTransformPoint(bones[n].position));
            var rotations=names.ToDictionary(n=>n,n=>Quaternion.Inverse(root.transform.rotation)*bones[n].rotation);
            var scales=names.ToDictionary(n=>n,n=>(root.transform.worldToLocalMatrix*bones[n].localToWorldMatrix).lossyScale);
            var curves=names.ToDictionary(n=>n,n=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray());
            const int samples=120;
            for(int i=0;i<=samples;i++)
            {
                float u=(float)i/samples,phase=2*Mathf.PI*u,t=duration*u;
                bool moving=name=="Walk" || name=="Run", run=name=="Run";
                Vector3 bodyOffset=Vector3.zero,bodyAngles=Vector3.zero;
                if(name=="Idle")bodyOffset.y=.0010f*Mathf.Sin(phase);
                if(moving){bodyOffset=new Vector3(-.0015f*Mathf.Cos(phase),.001f*(1-Mathf.Cos(phase*2)),0); bodyAngles=new Vector3(run?3:1,0,.7f*Mathf.Cos(phase));}
                float jump=name=="Jump"?Mathf.Sin(Mathf.PI*.5f*u):0,impact=name=="Land"?Mathf.Sin(Mathf.PI*u):0;
                if(name=="Jump")bodyAngles.x=-4*jump;
                if(name=="Fall")bodyAngles.x=2;
                if(name=="Land"){bodyOffset.y=-.006f*impact;bodyAngles.x=3*impact;}
                var poses=new Dictionary<string,Matrix4x4>(); var poseRot=new Dictionary<string,Quaternion>();
                foreach(string n in names)
                {
                    var bone=bones[n]; string parent=bone.parent.name;
                    Matrix4x4 parentRest=root.transform.worldToLocalMatrix*bone.parent.localToWorldMatrix;
                    Matrix4x4 parentPose=poses.ContainsKey(parent)?poses[parent]:parentRest;
                    Quaternion parentRotation=poseRot.ContainsKey(parent)?poseRot[parent]:Quaternion.Inverse(root.transform.rotation)*bone.parent.rotation;
                    Matrix4x4 inherited=parentPose*parentRest.inverse;
                    Vector3 position=inherited.MultiplyPoint3x4(rest[n]);
                    Quaternion rotation=inherited.rotation*rotations[n];
                    float sign=n.EndsWith("L",StringComparison.Ordinal)?1:-1;
                    if(n=="body"){position=rest[n]+bodyOffset;rotation=Quaternion.Euler(bodyAngles)*rotations[n];}
                    if(n=="head")rotation=Quaternion.Euler(0,0,moving?-.6f*Mathf.Cos(phase):.5f*Mathf.Sin(phase))*rotation;
                    if(n.StartsWith("arm"))
                    {
                        var angles=new Vector3(moving?(run?13:9)*Mathf.Cos(phase)*sign:-10*jump,0,sign*(moving?2:14*jump+6*impact));
                        rotation=Quaternion.Euler(angles)*rotation;
                    }
                    if(n.StartsWith("leg") || n.StartsWith("foot"))
                    {
                        string suffix=n.EndsWith("L",StringComparison.Ordinal)?"L":"R";
                        string foot="foot_"+suffix,leg="leg_"+suffix;
                        var gait=moving?PlushGait.Sample(u+(suffix=="L"?0:.5f),run?.52f:.28f,duration,run):new PlushGait.Foot(0,.010f*jump,-4*jump,true);
                        Vector3 target=rest[foot]+new Vector3(0,gait.Height,gait.Forward);
                        if(n.StartsWith("leg"))
                            rotation=Quaternion.FromToRotation(rest[foot]-rest[leg],target-position)*rotations[n];
                        else {position=target;rotation=Quaternion.Euler(gait.Pitch,0,0)*rotations[n];}
                    }
                    poses[n]=Matrix4x4.TRS(position,rotation,scales[n]); poseRot[n]=rotation;
                    Vector3 local=parentPose.inverse.MultiplyPoint3x4(position);
                    Quaternion q=Quaternion.Inverse(parentRotation)*rotation;
                    for(int c=0;c<3;c++)curves[n][c].AddKey(t,local[c]);
                    for(int c=0;c<4;c++)curves[n][c+3].AddKey(t,q[c]);
                }
            }
            foreach(string n in names)
            {
                string path=AnimationUtility.CalculateTransformPath(bones[n],root.transform);
                for(int c=0;c<7;c++)
                {
                    var curve=curves[n][c];
                    for(int k=0;k<curve.length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Linear);}
                    string property=c<3?"m_LocalPosition."+"xyz"[c]:"m_LocalRotation."+"xyzw"[c-3];
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),curve);
                }
            }
            clip.EnsureQuaternionContinuity(); AssetDatabase.CreateAsset(clip,output+"/"+name+".anim"); return clip;
        }
    }
}
