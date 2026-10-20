using UnityEngine;

namespace Lighting
{
    [CreateAssetMenu(menuName = "Lightfooted/Lighting/Light Profile", fileName = "NewLightProfile")]
    public sealed class LightProfileAsset : ScriptableObject
    {
        [SerializeField] private LightProfile profile = new();
        public LightProfile Profile => profile;
    }
}