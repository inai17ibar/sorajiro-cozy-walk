using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro
{
    [DefaultExecutionOrder(350)]
    public sealed class ComfortPlay : MonoBehaviour
    {
        public string lightingScene="Assets/Sorajiro/Generated/FleeceLighting_v002/L_LookDev.unity";
        public AudioClip[] cuteSteps;
        public AudioClip cuteLanding,cuteJump;
        public bool Ready { get; private set; }
        public int JumpSoundCount { get; private set; }
        public AudioSource JumpAudio { get; private set; }
        public OrbitCamera CameraRig { get; private set; }
        private StarPlaySession session;
        private AudioClip[] originalSteps;
        private AudioClip originalLanding;
        private bool previousGrounded,audioPaused,originalComfort;
        private float oldPitch,oldYaw,oldDistance;
        private string oldLabel;
        private SorajiroMotor oldCameraTarget;
        private Transform oldMotorView;
        private int reset;
        private IEnumerator Start()
        {
            if(!SceneManager.GetSceneByPath(lightingScene).isLoaded)
            {
                if(!Application.CanStreamedLevelBeLoaded(lightingScene)){Debug.LogError("Add lighting, Star Play and room scenes to the Build Profile");yield break;}
                yield return SceneManager.LoadSceneAsync(lightingScene,LoadSceneMode.Additive);
            }
            FleeceLighting light;
            while(!(session=FindAnyObjectByType<StarPlaySession>()) || !session.Ready || !(light=FindAnyObjectByType<FleeceLighting>()) || !light.Ready)yield return null;
            originalSteps=session.footsteps;originalLanding=session.landing;session.footsteps=cuteSteps;session.landing=cuteLanding;
            CameraRig=UnityEngine.Camera.main.GetComponent<OrbitCamera>();oldPitch=CameraRig.pitch;oldYaw=CameraRig.yaw;oldDistance=CameraRig.distance;originalComfort=CameraRig.comfortControls;
            // The preserved room was cloned root by root; cross-root camera references were lost.
            oldCameraTarget=CameraRig.target;oldMotorView=session.Motor.view;
            CameraRig.target=session.Motor;session.Motor.view=CameraRig.transform;
            CameraRig.ConfigureComfort();oldLabel=session.Motor.diagnosticLabel;session.Motor.diagnosticLabel="星を集めよう　Q/E：カメラ回転　C：くまの後ろへ　M：音";
            var g=new GameObject("Cute_Jump_Sound");g.transform.SetParent(transform,false);JumpAudio=g.AddComponent<AudioSource>();
            JumpAudio.playOnAwake=false;JumpAudio.spatialBlend=.15f;JumpAudio.minDistance=1;JumpAudio.maxDistance=7;JumpAudio.rolloffMode=AudioRolloffMode.Linear;
            previousGrounded=session.Motor.Grounded;reset=session.Motor.ResetVersion;Ready=true;
        }
        private void LateUpdate()
        {
            if(!Ready)return;
            var motor=session.Motor;JumpAudio.transform.position=motor.transform.position;JumpAudio.volume=session.Muted?0:session.Volume;
            bool paused=!session.IsActive;
            if(paused!=audioPaused){if(paused)JumpAudio.Pause();else JumpAudio.UnPause();audioPaused=paused;}
            bool returned=reset!=motor.ResetVersion;
            if(!paused && !returned && previousGrounded && !motor.Grounded && motor.VerticalSpeed>.1f)
            {JumpAudio.pitch=1;JumpAudio.PlayOneShot(cuteJump,.48f);JumpSoundCount++;}
            previousGrounded=motor.Grounded;reset=motor.ResetVersion;
        }
        private void OnDestroy()
        {
            if(session && originalSteps!=null){session.footsteps=originalSteps;session.landing=originalLanding;if(session.Motor){session.Motor.diagnosticLabel=oldLabel;session.Motor.view=oldMotorView;}}
            if(CameraRig){CameraRig.comfortControls=originalComfort;CameraRig.pitch=oldPitch;CameraRig.yaw=oldYaw;CameraRig.distance=oldDistance;CameraRig.target=oldCameraTarget;}
        }
    }
}
