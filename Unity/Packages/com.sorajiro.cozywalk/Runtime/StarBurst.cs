using UnityEngine;

namespace Sorajiro
{
    public sealed class StarBurst : MonoBehaviour
    {
        public StarPlaySession session;
        public Mesh mesh;
        public Material material;
        private readonly Transform[] sparks=new Transform[24];
        private readonly Vector3[] velocities=new Vector3[24];
        private float elapsed;
        private void Start()
        {
            for(int i=0;i<sparks.Length;i++)
            {
                var g=new GameObject("Golden_Spark");g.transform.SetParent(transform,false);
                g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
                sparks[i]=g.transform;float a=i*2.399963f,y=1-2*(i+.5f)/sparks.Length;
                float radius=Mathf.Sqrt(1-y*y);velocities[i]=new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius)*(.23f+.012f*(i%5))+Vector3.up*.13f;
            }
        }
        private void Update()
        {
            if(!session || !session.IsActive)return;
            elapsed+=Time.deltaTime;
            for(int i=0;i<sparks.Length;i++)
            {
                sparks[i].localPosition=velocities[i]*elapsed+Vector3.down*(.12f*elapsed*elapsed);
                float fade=Mathf.Clamp01(1-elapsed/.85f);
                sparks[i].localScale=Vector3.one*(.009f*fade*(.65f+.35f*Mathf.Sin(elapsed*32+i)));
                sparks[i].Rotate(50*Time.deltaTime,100*Time.deltaTime,0);
            }
            if(elapsed>=.85f)Destroy(gameObject);
        }
    }
}
