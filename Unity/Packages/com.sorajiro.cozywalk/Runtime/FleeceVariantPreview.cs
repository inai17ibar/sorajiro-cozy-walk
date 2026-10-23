using UnityEngine;
using UnityEngine.InputSystem;

namespace Sorajiro
{
    // Look-development comparison only. Shared imported rig and gameplay remain unchanged.
    public sealed class FleeceVariantPreview : MonoBehaviour
    {
        public SkinnedMeshRenderer body,pile;
        public Mesh baselinePile,groomedPile;
        public Material[] baselineBodyMaterials,baselinePileMaterials,fleeceBodyMaterials,fleecePileMaterials;
        [Range(0,2)] public int variant=2;
        private void Start(){Apply(variant);}
        public void Apply(int selection)
        {
            variant=Mathf.Clamp(selection,0,2);
            body.sharedMaterials=variant==0?baselineBodyMaterials:fleeceBodyMaterials;
            pile.sharedMaterials=variant==0?baselinePileMaterials:fleecePileMaterials;
            pile.sharedMesh=variant==2?groomedPile:baselinePile;
        }
        private void Update()
        {
            var key=Keyboard.current;if(key==null)return;
            if(key.digit1Key.wasPressedThisFrame)Apply(0);
            if(key.digit2Key.wasPressedThisFrame)Apply(1);
            if(key.digit3Key.wasPressedThisFrame)Apply(2);
        }
    }
}
