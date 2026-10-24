using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorajiro
{
    [DefaultExecutionOrder(300)]
    public sealed class FleeceLighting : MonoBehaviour
    {
        public string gameplayScene="Assets/Sorajiro/Generated/StarPlay_v003/L_LookDev.unity";
        public Light sideKey;
        public bool Ready { get; private set; }
        public int Variant { get; private set; }
        private Light[] originals;
        private float[] powers;
        private bool[] enabledStates;
        private IEnumerator Start()
        {
            if(!SceneManager.GetSceneByPath(gameplayScene).isLoaded)
            {
                if(!Application.CanStreamedLevelBeLoaded(gameplayScene)){Debug.LogError("Include the Star Play and room scenes in the Build Profile");yield break;}
                yield return SceneManager.LoadSceneAsync(gameplayScene,LoadSceneMode.Additive);
            }
            StarPlaySession session;
            while(!(session=FindAnyObjectByType<StarPlaySession>()) || !session.Ready)yield return null;
            var root=GameObject.Find("Preferred_BearFit_v001_3_Lights");
            string[] names={"Window_Side_Key","Window_Soft_Fill","Back_Soft_Rim","Room_Adaptation_Neutral_Fill"};
            originals=names.Select(n=>root.GetComponentsInChildren<Light>().Single(l=>l.name==n)).ToArray();
            powers=originals.Select(l=>l.intensity).ToArray();enabledStates=originals.Select(l=>l.enabled).ToArray();
            sideKey.transform.position=session.Motor.transform.position+new Vector3(-.42f,.48f,-.32f);
            sideKey.transform.LookAt(session.Motor.transform.position+Vector3.up*.25f);
            Ready=true;Apply(1);
        }
        public void Apply(int variant)
        {
            if(!Ready)return;
            Variant=variant;
            for(int i=0;i<originals.Length;i++){originals[i].enabled=enabledStates[i];originals[i].intensity=powers[i];}
            sideKey.enabled=variant!=0;
            if(variant==0)return;
            // Replace the strong frontal directional and broad fill with a grazing area key.
            originals[0].intensity=70;
            originals[1].intensity=18;
            originals[2].intensity=35;
            originals[3].intensity=variant==1?45:60;
            sideKey.intensity=variant==1?600:850;
        }
        private void OnDestroy()
        {
            if(originals==null)return;
            for(int i=0;i<originals.Length;i++)if(originals[i]){originals[i].intensity=powers[i];originals[i].enabled=enabledStates[i];}
        }
    }
}
