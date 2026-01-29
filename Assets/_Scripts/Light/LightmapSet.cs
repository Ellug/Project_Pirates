using UnityEngine;

[CreateAssetMenu(menuName = "Lighting/Lightmap Set")]
public class LightmapSet : ScriptableObject
{
    public LightmapData[] lightmaps;
    public LightProbes lightProbes;
}
