using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Sorajiro
{
    // Additive gameplay preserves the reviewed room's Scene GUID, lightmaps and APV.
    [DefaultExecutionOrder(200)]
    public sealed class StarPlaySession : MonoBehaviour
    {
        public string roomScene="Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity";
        public AudioClip[] footsteps;
        public AudioClip pickup,landing;
        public Mesh sparkMesh;
        public Material sparkMaterial;
        public GameObject replacementStars;
        public SorajiroMotor Motor { get; private set; }
        public AudioSource FootAudio { get; private set; }
        public AudioSource PickupAudio { get; private set; }
        public int CollectedCount=>collected.Count;
        public int TotalCount { get; private set; }
        public int FootstepCount { get; private set; }
        public int LandingCount { get; private set; }
        public int PickupSoundCount { get; private set; }
        public bool Ready { get; private set; }
        public bool IsActive=>Ready && Motor && Motor.Focused && !Motor.Paused;
        public float Volume { get; private set; }
        public bool Muted { get; private set; }
        private readonly HashSet<string> collected=new HashSet<string>();
        private readonly FootstepBeat beat=new FootstepBeat();
        private Animator animator;
        private bool audioPaused,wasGrounded;
        private float airTime;
        private int resetVersion,stepIndex;
        private string error;
        private const string VolumeKey="Sorajiro.Sfx.Volume.v1",MuteKey="Sorajiro.Sfx.Mute.v1";
        private IEnumerator Start()
        {
            Volume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,.65f));Muted=PlayerPrefs.GetInt(MuteKey,0)!=0;
            var scene=SceneManager.GetSceneByPath(roomScene);
            if(!scene.isLoaded)
            {
                if(!Application.CanStreamedLevelBeLoaded(roomScene)){error="部屋シーンをBuild Profileに追加してください";yield break;}
                yield return SceneManager.LoadSceneAsync(roomScene,LoadSceneMode.Additive);
                scene=SceneManager.GetSceneByPath(roomScene);
            }
            SceneManager.SetActiveScene(scene);
            if(!FindObjectsByType<AudioListener>().Any(a=>a.isActiveAndEnabled))Camera.main.gameObject.AddComponent<AudioListener>();
            // HDRP permits one shadow-casting directional light. Keep the selected bear key.
            foreach(var light in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()))
                if(light.type==LightType.Directional && light.name!="Window_Side_Key")light.shadows=LightShadows.None;
            Motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SorajiroMotor>()).Single();
            animator=Motor.GetComponent<BearAnimationDriver>().animator;resetVersion=Motor.ResetVersion;
            FootAudio=MakeAudio("Plush_Footsteps",.15f);PickupAudio=MakeAudio("Star_Chimes",.25f);
            var original=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Single(r=>r.name=="Room_StarGlow");
            original.enabled=false;replacementStars.SetActive(true);
            var stars=replacementStars.GetComponentsInChildren<StarPickup>();
            var roomLights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()).ToArray();
            foreach(var star in stars)
            {
                star.session=this;star.associatedLights=roomLights.Where(l=>l.type==LightType.Point&&l.name.Contains("Star")&&Vector3.Distance(l.transform.position,star.transform.position)<.22f).ToArray();
            }
            TotalCount=stars.Length;Ready=true;wasGrounded=Motor.Grounded;
            Motor.diagnosticLabel="星にふれて集めよう　M：音のオン／オフ";
        }
        private AudioSource MakeAudio(string name,float spatial)
        {
            var g=new GameObject(name);g.transform.SetParent(transform,false);var a=g.AddComponent<AudioSource>();
            a.playOnAwake=false;a.spatialBlend=spatial;a.minDistance=1;a.maxDistance=7;a.rolloffMode=AudioRolloffMode.Linear;
            a.volume=Muted?0:Volume;return a;
        }
        public void SetVolume(float value,bool muted)
        {
            Volume=Mathf.Clamp01(value);Muted=muted;
            if(FootAudio)FootAudio.volume=muted?0:Volume;if(PickupAudio)PickupAudio.volume=muted?0:Volume;
            PlayerPrefs.SetFloat(VolumeKey,Volume);PlayerPrefs.SetInt(MuteKey,muted?1:0);PlayerPrefs.Save();
        }
        public bool Collect(string id,Vector3 position)
        {
            if(!IsActive || string.IsNullOrEmpty(id) || !collected.Add(id))return false;
            var g=new GameObject("Star_Pickup_Burst");SceneManager.MoveGameObjectToScene(g,gameObject.scene);g.transform.position=position;
            var burst=g.AddComponent<StarBurst>();burst.session=this;burst.mesh=sparkMesh;burst.material=sparkMaterial;
            PickupAudio.transform.position=position;PickupAudio.pitch=1+.04f*(CollectedCount-1);PickupAudio.PlayOneShot(pickup,.55f);PickupSoundCount++;
            return true;
        }
        private void Update()
        {
            if(!Ready)return;
            if(Keyboard.current!=null && Motor.Focused && Keyboard.current.mKey.wasPressedThisFrame)SetVolume(Volume,!Muted);
            bool paused=!IsActive;
            if(paused!=audioPaused)
            {foreach(var source in new[]{FootAudio,PickupAudio}){if(paused)source.Pause();else source.UnPause();}audioPaused=paused;}
            if(paused){beat.Advance(0,false);airTime=0;wasGrounded=Motor.Grounded;return;}
            FootAudio.transform.position=Motor.transform.position;
            bool reset=resetVersion!=Motor.ResetVersion;
            if(reset){resetVersion=Motor.ResetVersion;airTime=0;beat.Advance(0,false);wasGrounded=Motor.Grounded;}
            if(!Motor.Grounded)airTime+=Time.deltaTime;
            if(!reset && !wasGrounded && Motor.Grounded && airTime>.08f)
            {FootAudio.pitch=.96f;FootAudio.PlayOneShot(landing,.55f);LandingCount++;airTime=0;}
            wasGrounded=Motor.Grounded;
            var state=animator.GetCurrentAnimatorStateInfo(0);
            bool moving=Motor.Grounded && Motor.PlanarSpeed>.045f && state.IsName("Locomotion") && !animator.IsInTransition(0);
            if(beat.Advance(state.normalizedTime,moving))
            {FootAudio.pitch=.96f+.025f*(stepIndex%4);FootAudio.PlayOneShot(footsteps[stepIndex++%footsteps.Length],Motor.PlanarSpeed>Motor.walkSpeed*1.15f?.55f:.38f);FootstepCount++;}
        }
        private void OnGUI()
        {
            if(!Ready){GUI.Label(new Rect(20,170,650,40),error??"星の部屋を準備しています…");return;}
            float x=Mathf.Max(12,Screen.width-266);
            GUI.Box(new Rect(x,12,250,98),GUIContent.none);
            GUI.Label(new Rect(x+12,20,226,24),CollectedCount==TotalCount?"星がそろいました！":"星  "+CollectedCount+" / "+TotalCount);
            if(GUI.Button(new Rect(x+12,49,92,25),Muted?"音：オフ":"音：オン"))SetVolume(Volume,!Muted);
            GUI.Label(new Rect(x+114,45,116,20),"効果音量");
            float volume=GUI.HorizontalSlider(new Rect(x+114,70,118,20),Volume,0,1);
            if(Mathf.Abs(volume-Volume)>.005f)SetVolume(volume,Muted);
            GUI.Label(new Rect(x+12,83,226,22),"Rで戻っても取得数はそのまま");
        }
    }
}
