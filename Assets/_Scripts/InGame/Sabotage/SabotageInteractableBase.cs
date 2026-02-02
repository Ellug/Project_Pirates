using Photon.Pun;
using UnityEngine;
using System.Collections;

// 사보타지 해제용 상호작용 베이스
// CCTV 의 IsInteractable 기반으로 작성
public class SabotageInteractableBase : InteractionObject
{
    [Header("Sabotage")]
    [SerializeField] protected SabotageManager _sabotageManager;
    [SerializeField] protected SabotageId _targetId;

    [Header("Net")]
    [SerializeField] protected PhotonView _pv;

    [Header("Hold")]
    [SerializeField] protected float _holdSeconds = 2f;

    protected virtual void Awake()
    {
        if (_pv == null) _pv = GetComponent<PhotonView>();
    }

    protected int GetActorNumber(PlayerInteraction player)
    {
        PhotonView pv = player.GetComponentInParent<PhotonView>();
        return pv.OwnerActorNr;
    }

    // 현재 이 오브젝트의 사보타지가 활성 상태인지?
    protected bool IsTargetActive()
    {
        if (_sabotageManager == null) return false;
        return (_sabotageManager.IsActive && _sabotageManager.ActiveSabotage == _targetId);
    }

    // 상호작용을 시도한 플레이어가 마피아인지?
    protected bool IsMafia(PlayerInteraction player)
    {
        var pc = player.GetComponentInParent<PlayerController>();
        return (pc != null && pc.isMafia);
    }

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (!IsTargetActive()) return;
        if (player == null) return;
        if (IsMafia(player)) return;

        StartCoroutine(HoldRoutine(player, rpcManager)); // 조건 통과시 2초 누르기 시작
    }

    private IEnumerator HoldRoutine(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        float t = 0f; // 누적 시간

        while (t < _holdSeconds)
        {
            // 사보타지가 꺼졌거나 다른 사보타지면 취소
            if(!IsTargetActive())
            {
                OnHoldCanceled(player);
                yield break;
            }

            // 상호작용 불가일때 (레이 밖으로 벗어날때)
            if(player.IsInteractable == false)
            {
                OnHoldCanceled(player);
                yield break;
            }

            // 플레이어가 바라보고 있는 대상이 이 오브젝트가 아니면?
            if(player.CurrentInteractable != this)
            {
                OnHoldCanceled(player);
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }
        // 성공 처리
        OnHoldSuccess(player, rpcManager);
    }

    protected virtual void OnHoldSuccess(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        Debug.Log($"[Sabotage] Hold Success : {_targetId}");
    }

    protected virtual void OnHoldCanceled(PlayerInteraction player)
    {
        Debug.Log($"[Sabotage] Hold Cnaceled : {_targetId}");
    }
}
