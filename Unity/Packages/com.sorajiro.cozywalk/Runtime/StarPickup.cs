using UnityEngine;

namespace Sorajiro
{
    public sealed class StarPickup : MonoBehaviour
    {
        public StarPlaySession session;
        public Renderer visual;
        public string starId;
        public Light[] associatedLights;
        public bool Collected { get; private set; }
        private void OnTriggerEnter(Collider other){TryCollect(other);}
        private void OnTriggerStay(Collider other){TryCollect(other);}
        public bool TryCollect(Collider other)
        {
            if(Collected || !session || !other || other.GetComponentInParent<SorajiroMotor>()!=session.Motor || !session.IsActive)return false;
            if(!session.Collect(starId,transform.position))return false;
            Collected=true;GetComponent<Collider>().enabled=false;visual.enabled=false;
            if(associatedLights!=null)foreach(var light in associatedLights)if(light)light.enabled=false;
            return true;
        }
    }
}
