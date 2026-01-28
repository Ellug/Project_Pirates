using UnityEngine;

public class PowerSystem : MonoBehaviour
{
    public BlackoutController blackout;
    public RealtimeLightGroup lightGroup;

    public void PowerOff()
    {
        Debug.Log("[PowerSystem] Power OFF");

        if (lightGroup != null)
            lightGroup.SetLights(false);

        if (blackout != null)
            blackout.StartBlackout();
    }

    public void PowerOn()
    {
        Debug.Log("[PowerSystem] Power ON");

        if (lightGroup != null)
            lightGroup.SetLights(true);

        if (blackout != null)
            blackout.RestoreLight();
    }
}
