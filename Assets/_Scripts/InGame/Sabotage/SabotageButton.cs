using UnityEngine;

public class SabotageButton : MonoBehaviour
{
    [SerializeField] private SabotageManager _sabotageManager;
    [SerializeField] private SabotageId _sabotageId;

    public void Trigger()
    {
        Debug.Log($"[UI] Sabotage Button Clicked : {_sabotageId}");

        if (_sabotageManager == null) return;

        _sabotageManager.RequestTriggerSabotage(_sabotageId);
    }
}
