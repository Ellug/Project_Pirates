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

    // 동시 홀드 상태 (모든 클라이언트에서 동기화)
    public bool IsSimultaneousHold { get; private set; }
    public float SimultaneousHoldProgress { get; private set; } // 0~1 진행도

    // 각 콘솔의 홀드 상태 (MasterClient에서만 관리)
    private int _console1HoldingActor = -1;
    private int _console2HoldingActor = -1;

    // 동시 홀드 시작 시간
    private double _simultaneousStartTime = -1;

    void Awake()
    {
        if (_pv == null) _pv = GetComponent<PhotonView>();
        if (_pv == null)
        {
            Debug.LogError("[EngineSabotageManager] PhotonView가 없습니다! 이 오브젝트에 PhotonView를 추가하세요.");
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_sabotageManager == null) return;
        if (!_sabotageManager.IsActive || _sabotageManager.ActiveSabotage != SabotageId.Engine) return;

        // 두 콘솔 모두 홀드 중이고, 서로 다른 플레이어인지 확인
        bool bothHolding = _console1HoldingActor > 0 && _console2HoldingActor > 0;
        bool differentPlayers = _console1HoldingActor != _console2HoldingActor;
        bool shouldBeSimultaneous = bothHolding && differentPlayers;

        // 동시 홀드 상태 변경 감지
        if (shouldBeSimultaneous && !IsSimultaneousHold)
        {
            // 동시 홀드 시작
            _simultaneousStartTime = PhotonNetwork.Time;
            Debug.Log($"[EngineSabotage] 동시 홀드 시작! Console1: Actor{_console1HoldingActor}, Console2: Actor{_console2HoldingActor}");
            _pv.RPC(nameof(RPC_SetSimultaneousHold), RpcTarget.All, true, 0f);
        }
        else if (!shouldBeSimultaneous && IsSimultaneousHold)
        {
            // 동시 홀드 종료
            Debug.Log($"[EngineSabotage] 동시 홀드 중단! Console1: {_console1HoldingActor}, Console2: {_console2HoldingActor}");
            _simultaneousStartTime = -1;
            _pv.RPC(nameof(RPC_SetSimultaneousHold), RpcTarget.All, false, 0f);
        }

        // 동시 홀드 중이면 진행도 업데이트
        if (shouldBeSimultaneous && _simultaneousStartTime > 0)
        {
            double elapsed = PhotonNetwork.Time - _simultaneousStartTime;
            float progress = Mathf.Clamp01((float)elapsed / _simultaneousHoldTime);

            _pv.RPC(nameof(RPC_SetSimultaneousHold), RpcTarget.All, true, progress);

            if (elapsed >= _simultaneousHoldTime)
            {
                Debug.Log("[EngineSabotage] 동시 홀드 성공! 사보타지 해제!");
                _sabotageManager.RequestResolveSabotage(SabotageId.Engine);
                ResetHoldState();
                _pv.RPC(nameof(RPC_SetSimultaneousHold), RpcTarget.All, false, 0f);
            }
        }
    }

    [PunRPC]
    private void RPC_SetSimultaneousHold(bool isHolding, float progress)
    {
        IsSimultaneousHold = isHolding;
        SimultaneousHoldProgress = progress;
    }

    // 콘솔에서 홀드 시작/종료 시 호출
    public void ReportHoldState(int consoleIndex, int actorNumber, bool isHolding)
    {
        Debug.Log($"[EngineSabotage] ReportHoldState - Console{consoleIndex}, Actor{actorNumber}, Holding: {isHolding}");
        _pv.RPC(nameof(RPC_ReportHoldState), RpcTarget.MasterClient, consoleIndex, actorNumber, isHolding);
    }

    [PunRPC]
    private void RPC_ReportHoldState(int consoleIndex, int actorNumber, bool isHolding)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int actor = isHolding ? actorNumber : -1;

        if (consoleIndex == 0)
        {
            _console1HoldingActor = actor;
            Debug.Log($"[EngineSabotage] Master: Console1 = Actor{actor}");
        }
        else if (consoleIndex == 1)
        {
            _console2HoldingActor = actor;
            Debug.Log($"[EngineSabotage] Master: Console2 = Actor{actor}");
        }

        Debug.Log($"[EngineSabotage] 현재 상태 - Console1: {_console1HoldingActor}, Console2: {_console2HoldingActor}");
    }

    private void ResetHoldState()
    {
        _console1HoldingActor = -1;
        _console2HoldingActor = -1;
        _simultaneousStartTime = -1;
    }
}
