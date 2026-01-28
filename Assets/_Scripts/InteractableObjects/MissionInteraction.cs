using UnityEngine;

public class MissionInteraction : InteractionObject
{
    [Header("Mission Container Index")]
    [SerializeField] private int _missionIndex = 0;


    private bool _alreadyCleared = false;  // 이미 클리어했는지 여부 (누군가 클리어한건 또 수행 불가)
    private bool _isUsing = false;         // 누군가 상호작용 중인지 여부 (중복 수행 불가)

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (_alreadyCleared)
            return;
        if (_isUsing)
            return;

        _isUsing = true;
        MissionContainer.Instance.StartMission(_missionIndex);
    }

    // 다른 사람에게 변화가 생기는건
    public override void OnOthersInteract()
    {
        
    }
}
