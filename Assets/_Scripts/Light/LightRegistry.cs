using System.Collections.Generic;

public static class LightRegistry
{
    private static readonly HashSet<ProximityLight> allLights = new HashSet<ProximityLight>();
    public static IReadOnlyCollection<ProximityLight> AllLights => allLights;

    public static void Register(ProximityLight light)
    {
        if (light != null)
            allLights.Add(light);
    }

    public static void Unregister(ProximityLight light)
    {
        if (light != null)
            allLights.Remove(light);
    }
}
