using Photon.Pun;
using UnityEngine;

// 전기실 : 2초 유지 성공하면 해제
public class PowerSabotageConsole : SabotageInteractableBase
{
    protected override void OnHoldSuccess(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (!IsTargetActive()) return;
        if (IsMafia(player)) return;

        int actor = GetActorNumber(player);
        if (actor <= 0) return;

        _pv.RPC(nameof(RPC_RequestResolvePower), RpcTarget.MasterClient, actor);
    }

    [PunRPC]
    private void RPC_RequestResolvePower(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsTargetActive()) return;

        _sabotageManager.RequestResolveSabotage(_targetId);
    }
}
