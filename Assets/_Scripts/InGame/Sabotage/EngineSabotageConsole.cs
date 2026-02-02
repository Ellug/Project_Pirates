using Photon.Pun;
using UnityEngine;

// 엔진실 : 2명이 각각 2초 유지 성공해야 해제
// 첫번째 상호작용 성공자의 시간을 측정
// 두번째 상호작용 성공자가 첫번째 시간에서 2초 내로 상호작용시 해제하는 로직
public class EngineSabotageConsole : SabotageInteractableBase
{
    [Header("Engine Rule")]
    [SerializeField] private float _pairSeconds = 2f;

    private int _firstActor = -1;
    private double _firstTime = -1;

    protected override void OnHoldSuccess(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (!IsTargetActive()) return;
        if (IsMafia(player)) return;

        // 홀드에 성공한 플레이어의 Actor 가져옴
        int actor = GetActorNumber(player);
        if (actor <= 0) return;

        _pv.RPC(nameof(RPC_RequestEngineHoldDone), RpcTarget.MasterClient, actor);
    }

    [PunRPC]
    private void RPC_RequestEngineHoldDone(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || !IsTargetActive()) return;

        double now = PhotonNetwork.Time;
        bool expired = _firstActor < 0 || (now - _firstTime) > _pairSeconds;

        if(expired)
        {
            _firstActor = actorNumber;
            _firstTime = now;
            return;
        }

        if (_firstActor == actorNumber) return;

        _firstActor = -1;
        _firstTime = -1;

        _sabotageManager.RequestResolveSabotage(_targetId);
    }
}
