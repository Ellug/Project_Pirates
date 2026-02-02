using Photon.Pun;
using UnityEngine;
using System.Collections;

// 엔진 사보타지 콘솔: 플레이어가 홀드하면 매니저에 상태 보고
public class EngineSabotageConsole : InteractionObject
{
    [Header("Sabotage")]
    [SerializeField] private SabotageManager _sabotageManager;
    [SerializeField] private EngineSabotageManager _engineManager;

    [Header("Console Config")]
    [SerializeField] private int _consoleIndex; // 0 1

    [Header("Hold")]
    [SerializeField] private float _holdCheckInterval = 0.1f;

    private Coroutine _holdCoroutine;
    private int _currentHoldingActor = -1;

    private bool IsTargetActive()
    {
        if (_sabotageManager == null) return false;
        return _sabotageManager.IsActive && _sabotageManager.ActiveSabotage == SabotageId.Engine;
    }

    private int GetActorNumber(PlayerInteraction player)
    {
        PhotonView pv = player.GetComponentInParent<PhotonView>();
        return pv != null ? pv.OwnerActorNr : -1;
    }

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (!IsTargetActive()) return;
        if (player == null) return;

        int actorNumber = GetActorNumber(player);
        if (actorNumber <= 0) return;

        // 이미 홀드 중이면 무시
        if (_holdCoroutine != null) return;

        _holdCoroutine = StartCoroutine(HoldRoutine(player, actorNumber));
    }

    private IEnumerator HoldRoutine(PlayerInteraction player, int actorNumber)
    {
        _currentHoldingActor = actorNumber;
        _engineManager.ReportHoldState(_consoleIndex, actorNumber, true);

        var interval = new WaitForSeconds(_holdCheckInterval);

        while (true)
        {
            // 사보타지가 끝났으면 종료
            if (!IsTargetActive())
                break;

            // 상호작용 불가 상태면 종료
            if (!player.IsInteractable)
                break;

            // 다른 오브젝트를 보고 있으면 종료
            if (player.CurrentInteractable != this)
                break;

            yield return interval;
        }

        // 홀드 종료 보고
        _engineManager.ReportHoldState(_consoleIndex, actorNumber, false);
        _currentHoldingActor = -1;
        _holdCoroutine = null;
    }

    private void OnDisable()
    {
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            if (_currentHoldingActor > 0)
            {
                _engineManager.ReportHoldState(_consoleIndex, _currentHoldingActor, false);
            }
            _currentHoldingActor = -1;
            _holdCoroutine = null;
        }
    }
}
