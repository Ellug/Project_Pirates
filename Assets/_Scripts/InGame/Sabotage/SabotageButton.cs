using UnityEngine;

public class SabotageButton : MonoBehaviour
{
    [SerializeField] private SabotageManager _sabotageManager;
    [SerializeField] private SabotageId _sabotageId;

    public void Trigger()
    {
        Debug.Log($"[UI] Sabotage Button Clicked : {_sabotageId}");

        if(_sabotageManager.IsActive)
        {
            Debug.Log("[UI] Sabotage already active");
            return;
        }

        bool success = _sabotageManager.TriggerSabotage(_sabotageId);

        if(!success)
        {
            Debug.Log("[UI] Sabotage trigger failed");
            return;
        }
    }
}
