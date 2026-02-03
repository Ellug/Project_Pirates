using UnityEngine;
using UnityEngine.Rendering;

public class LightingSwitchManager : MonoBehaviour
{
    [Header("Lightmap Sets")]
    public LightmapSet lightOnSet;
    public LightmapSet lightOffSet;

    [Header("APV Lighting Scenarios")]
    public string lightOnScenario = "LightOn";
    public string lightOffScenario = "LightOff";

    void Start()
    {
        ApplyLightOnImmediate();
    }
    public void ApplyLightOnImmediate()
    {
        ApplyLightmap(lightOnSet);

        if (ProbeReferenceVolume.instance != null)
            ProbeReferenceVolume.instance.lightingScenario = lightOnScenario;
    }
    public void ApplyLightOffImmediate()
    {
        ApplyLightmap(lightOffSet);

        if (ProbeReferenceVolume.instance != null)
            ProbeReferenceVolume.instance.lightingScenario = lightOffScenario;
    }
    private void ApplyLightmap(LightmapSet set)
    {
        if (set == null)
        {
            Debug.LogError("LightmapSet missing");
            return;
        }

        LightmapSettings.lightmaps = set.lightmaps;
        LightmapSettings.lightProbes = set.lightProbes;

        DynamicGI.UpdateEnvironment();
    }
}
