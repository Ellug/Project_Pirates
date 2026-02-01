using UnityEngine;

public class MissionInteraction : InteractionObject
{
    [Header("Mission Container Index")]
    [SerializeField] private int _missionIndex = 0;
    private InteractionObjectRpcManager _rpcManager;

    [HideInInspector] public bool alreadyCleared = false;  // 이미 클리어했는지 여부 (누군가 클리어한건 또 수행 불가)
    [HideInInspector] public bool isUsing = false;         // 누군가 상호작용 중인지 여부 (중복 수행 불가)

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (alreadyCleared)
            return;
        if (isUsing)
            return;

        if (_rpcManager == null)
            _rpcManager = rpcManager;

        _rpcManager.RequestNetworkInteraction(uniqueID);
        isUsing = !isUsing;

        MissionContainer.Instance.StartMission(_missionIndex, this, player.transform);
    }

    // 다른 사람에게 변화가 생기는건
    public override void OnOthersInteract()
    {
        isUsing = !isUsing;
    }

    // 모두가 RPC를 받아서 클리어했음을 설정하는 메서드
    public void SetCleared()
    {
        alreadyCleared = true;
        Renderer mat = GetComponent<Renderer>();
        mat.material = MissionContainer.Instance.clearMaterial;
    }

    public void ExitUse()
    {
        _rpcManager.RequestNetworkInteraction(uniqueID);
        isUsing = !isUsing;
    }

    // 로컬에서 실행되어 모두에게 알리는 메서드
    public void MissionCleared()
    {
        _rpcManager.RequestNetworkMissionCleared(uniqueID);
    }
}
