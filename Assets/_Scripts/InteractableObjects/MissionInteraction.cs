using UnityEngine;

public class MissionInteraction : InteractionObject
{
    [Header("Mission Container Index")]
    [SerializeField] private int _missionIndex = 0;


    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        
    }

    public override void OnOthersInteract()
    {
        
    }
}
