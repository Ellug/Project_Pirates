using Photon.Pun;
using UnityEngine;

// 엔진 사보타지: 2개의 콘솔을 2명이 동시에 2초간 홀드해야 해제
public class EngineSabotageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SabotageManager _sabotageManager;
    [SerializeField] private PhotonView _pv;

    [Header("Config")]
    [SerializeField] private float _simultaneousHoldTime = 2f;

    // 각 콘솔의 홀드 상태 (actorNumber, -1이면 홀드 안함)
    private int _console1HoldingActor = -1;
    private int _console2HoldingActor = -1;

    // 동시 홀드 시작 시간
    private double _simultaneousStartTime = -1;

    private void Awake()
    {
        if (_pv == null) _pv = GetComponent<PhotonView>();
        if (_pv == null)
        {
            Debug.LogError("[EngineSabotageManager] PhotonView가 없습니다! 이 오브젝트에 PhotonView를 추가하세요.");
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_sabotageManager == null) return;
        if (!_sabotageManager.IsActive || _sabotageManager.ActiveSabotage != SabotageId.Engine) return;

        // 두 콘솔 모두 홀드 중이고, 서로 다른 플레이어인지 확인
        bool bothHolding = _console1HoldingActor > 0 && _console2HoldingActor > 0;
        bool differentPlayers = _console1HoldingActor != _console2HoldingActor;

        if (bothHolding && differentPlayers)
        {
            if (_simultaneousStartTime < 0)
            {
                _simultaneousStartTime = PhotonNetwork.Time;
            }

            double elapsed = PhotonNetwork.Time - _simultaneousStartTime;
            if (elapsed >= _simultaneousHoldTime)
            {
                _sabotageManager.RequestResolveSabotage(SabotageId.Engine);
                ResetHoldState();
            }
        }
        else
        {
            _simultaneousStartTime = -1;
        }
    }

    // 콘솔에서 홀드 시작/종료 시 호출
    public void ReportHoldState(int consoleIndex, int actorNumber, bool isHolding)
    {
        _pv.RPC(nameof(RPC_ReportHoldState), RpcTarget.MasterClient, consoleIndex, actorNumber, isHolding);
    }

    [PunRPC]
    private void RPC_ReportHoldState(int consoleIndex, int actorNumber, bool isHolding)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int actor = isHolding ? actorNumber : -1;

        if (consoleIndex == 0)
            _console1HoldingActor = actor;
        else if (consoleIndex == 1)
            _console2HoldingActor = actor;

        // 홀드가 끊기면 타이머 리셋
        if (!isHolding)
            _simultaneousStartTime = -1;
    }

    private void ResetHoldState()
    {
        _console1HoldingActor = -1;
        _console2HoldingActor = -1;
        _simultaneousStartTime = -1;
    }
}
