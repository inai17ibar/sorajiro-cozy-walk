using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sorajiro
{
    // Captures actual Play-mode frames. Scripted input; capture overhead is not a benchmark.
    [DefaultExecutionOrder(200)]
    public sealed class MotionCaptureProbe : MonoBehaviour
    {
        private SorajiroMotor motor;
        private BearAnimationDriver driver;
        private FleeceVariantPreview fleece;
        private BearFitPreview fit;
        private OrbitCamera orbit;
        private RenderTexture target;
        private Texture2D pixels;
        private StreamWriter log;
        private string folder;
        private float start,nextCapture,oldYaw,oldPitch,oldDistance;
        private bool oldMotorEnabled,jump1,jump2;
        private Quaternion oldRotation;
        private int frame;

        public void Initialize(SorajiroMotor source,string outputFolder)
        {
            motor=source; fit=motor.GetComponent<BearFitPreview>(); fleece=motor.GetComponent<FleeceVariantPreview>(); driver=motor.GetComponent<BearAnimationDriver>(); orbit=Camera.main.GetComponent<OrbitCamera>();
            var capture=this;
            capture.oldMotorEnabled=motor.enabled; capture.oldRotation=motor.visualRoot.rotation;
            capture.oldYaw=capture.orbit.yaw; capture.oldPitch=capture.orbit.pitch; capture.oldDistance=capture.orbit.distance;
            motor.enabled=false; motor.ReturnToStart();
            capture.folder=outputFolder; Directory.CreateDirectory(capture.folder);
            capture.target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){useDynamicScale=false}; capture.target.Create();
            capture.pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false,false);
            capture.log=new StreamWriter(Path.Combine(capture.folder,"frames.csv")); capture.log.WriteLine("frame,time_s,world_x,world_y,world_z,planar_mps,vertical_mps,grounded,animator_state_hash,fleece_variant,pile_vertices,fit_variant,fit_active_model,visual_model");
            capture.start=Time.time; capture.nextCapture=0;
            Debug.Log("SORAJIRO MOTION START: "+capture.folder+"; actual Play + HDRP camera requests; scripted input, 15fps sampling, no performance claim.");
        }

        private void Update()
        {
            if(!motor) return;
            float t=Time.time-start; float forward=0,right=0; bool run=false,jump=false;
            if(t>=5 && t<10)forward=1;
            if(t>=10.5f && t<15.5f)forward=-1;
            if(t>=16 && !jump1){jump1=true;jump=true;}
            if(t>=18 && !jump2){jump2=true;jump=true;}
            if(t>=20 && t<21.5f){right=1;run=true;}
            if(t>=22 && t<23.5f){right=-1;run=true;}
            orbit.yaw=oldYaw+(t>=24?Mathf.Clamp01((t-24)/3)*70:0);
            orbit.distance=Mathf.Lerp(oldDistance,.75f,Mathf.Clamp01((t-27)/3));
            motor.Step(right,forward,jump,run,Time.deltaTime); driver.Sync(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if(!motor || !target) return;
            try
            {
                float elapsed=Time.time-start;
                if(elapsed>=30)
                {
                    log.Flush();
                    File.WriteAllText(Path.Combine(folder,"complete.txt"),"Actual Unity Play-mode frames; 1920x1080 camera StandardRequest; target sampling 15fps, timestamps in frames.csv. Scripted movement + jump + orbit + zoom. No frame-rate benchmark or owner-quality approval.\nScene="+gameObject.scene.path+"\nUnity="+Application.unityVersion+"; GPU="+SystemInfo.graphicsDeviceName+"; API="+SystemInfo.graphicsDeviceType+"; captured frames="+frame+"; simulation seconds="+elapsed+"\n");
                    Debug.Log("SORAJIRO MOTION COMPLETE: "+folder+"; frames="+frame); Destroy(gameObject); return;
                }
                if(elapsed<nextCapture) return;
                var camera=Camera.main; float aspect=camera.aspect; RenderTexture previous=RenderTexture.active;
                try
                {
                    camera.aspect=16f/9;
                    var request=new RenderPipeline.StandardRequest{destination=target};
                    if(!RenderPipeline.SupportsRenderRequest(camera,request)) throw new Exception("HDRP capture request unavailable.");
                    RenderPipeline.SubmitRenderRequest(camera,request); RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder,frame.ToString("D5")+".jpg"),pixels.EncodeToJPG(95));
                }
                finally {RenderTexture.active=previous; camera.aspect=aspect;}
                Vector3 p=motor.transform.position;
                log.WriteLine(FormattableString.Invariant($"{frame},{elapsed:F6},{p.x:F6},{p.y:F6},{p.z:F6},{motor.PlanarSpeed:F6},{motor.VerticalSpeed:F6},{motor.Grounded},{driver.animator.GetCurrentAnimatorStateInfo(0).shortNameHash},{(fleece?fleece.variant:-1)},{(fleece?fleece.pile.sharedMesh.vertexCount:0)},{(fit?fit.variant:-1)},{(fit?fit.motor.visualRoot.name:"none")},{motor.visualRoot.name}"));
                frame++; nextCapture=elapsed+1f/15;
            }
            catch(Exception e){Debug.LogException(e);Destroy(gameObject);}
        }

        private void OnDestroy()
        {
            log?.Dispose();
            if(motor){motor.ReturnToStart(); motor.visualRoot.rotation=oldRotation; motor.enabled=oldMotorEnabled;}
            if(orbit){orbit.yaw=oldYaw; orbit.pitch=oldPitch; orbit.distance=oldDistance;}
            if(target){target.Release(); Destroy(target);}
            if(pixels)Destroy(pixels);
        }
    }
}
