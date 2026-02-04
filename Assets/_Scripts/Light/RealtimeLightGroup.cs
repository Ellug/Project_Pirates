using UnityEngine;

public class RealtimeLightGroup : MonoBehaviour
{
    public bool IsPowerOn { get; private set; } = true;

    public void SetPower(bool on)
    {
        IsPowerOn = on;
        Debug.Log($"[RealtimeLightGroup] Power = {on}");
    }
}
