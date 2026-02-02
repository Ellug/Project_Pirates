using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

// 트리거 되는 순간 코루틴으로 waitforsec 로 시간 카운트 
// 중간에 시민이 활성화하면 yeild break; 
// 타이머 초기화 
// 코루틴을 빠져나오면 false = 마피아 승리

public enum SabotageId
{
    None,
    Engine,
    Light
}

public class SabotageManager : MonoBehaviour
{
    [Header("State")]
    public bool IsActive => _isActive;
    public SabotageId ActiveSabotage => _activeId;
    public float RemainingTime => _remainingTime;

    [Header("Config")]
    [SerializeField] private float _defaultDuration = 60f; // 사보타지 제한시간

    [Header("Countdown UI")]
    [SerializeField] private GameObject _countdownPanel;
    [SerializeField] private TMP_Text _countdownText;

    [Header("Net")]
    [SerializeField] private PhotonView _pv;

    [Header("Sabotage References")]
    [SerializeField] private BlackoutPropertyBinder _blackoutBinder;
    [SerializeField] private SabotageButton _sabotageButton;

    private bool _isActive; // 사보타지가 진행중인지 판단
    private SabotageId _activeId = SabotageId.None;
    
    private float _remainingTime;
    private float _activeDuration; // UI 표시용

    private double _startServerTime; // PhotonNetwork 기준 시작 시간
    private Coroutine _countdownCor;

    private void Awake()
    {
        if (_pv == null) _pv = GetComponent<PhotonView>();
        SetCountdownUI(false);

        if (_sabotageButton != null)
            _sabotageButton.SetButtonsActive(false);
    }

    // 마피아 전용 버튼 활성화 (PlayerController.IsMafia에서 호출)
    public void EnableMafiaButtons()
    {
        if (_sabotageButton != null)
            _sabotageButton.SetButtonsActive(true);
    }

    // 마피아 버튼이 호출 : Master 에게 사보타지 시작 요청
    public void RequestTriggerSabotage(SabotageId id, float duration = -1f)
    {
        if (!PhotonNetwork.InRoom) return;
        if (id == SabotageId.None) return;

        _pv.RPC(nameof(RPC_RequestStart), RpcTarget.MasterClient, (int)id, duration);
    }

    // 시민 상호작용이 호출 : Master 에게 해결 요청
    public void RequestResolveSabotage(SabotageId id)
    {
        if (!PhotonNetwork.InRoom) return;
        if (id == SabotageId.None) return;

        _pv.RPC(nameof(RPC_RequestResolve), RpcTarget.MasterClient, (int)id);
    }

    [PunRPC]
    private void RPC_RequestStart(int idRaw, float duration)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var id = (SabotageId)idRaw;
        if (id == SabotageId.None) return;

        // Light는 타이머 없이 즉시 효과만 적용
        if (id == SabotageId.Light)
        {
            _pv.RPC(nameof(RPC_TriggerLight), RpcTarget.All, true);
            return;
        }

        // Engine 등 타이머 사보타지
        if (_isActive) return;

        float dur = (duration > 0f) ? duration : _defaultDuration;
        double startTime = PhotonNetwork.Time;

        _pv.RPC(nameof(RPC_StartSabotage), RpcTarget.All, idRaw, dur, startTime);
    }

    [PunRPC]
    private void RPC_RequestResolve(int idRaw) // Master 가 해결 가능한 상태인지 검증
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var id = (SabotageId)idRaw;

        // Light는 타이머 없이 즉시 해제
        if (id == SabotageId.Light)
        {
            _pv.RPC(nameof(RPC_TriggerLight), RpcTarget.All, false);
            return;
        }

        // Engine 등 타이머 사보타지
        if (!_isActive) return;
        if (_activeId != id) return;

        _pv.RPC(nameof(RPC_ResolveSabotage), RpcTarget.All, idRaw);
    }

    [PunRPC]
    private void RPC_StartSabotage(int idRaw, float duration, double startSeverTime) // 모든 클라에서 동일하게 실행
    {
        var id = (SabotageId)idRaw;

        StopCountdownCoroutine();

        _isActive = true;
        _activeId = id;

        _activeDuration = duration;
        _startServerTime = startSeverTime;
        
        _remainingTime = duration;

        OnSabotageStart(id);
        StartCountdownCoroutine();
    }

    [PunRPC]
    private void RPC_ResolveSabotage(int idRaw)
    {
        var id = (SabotageId)idRaw;
        if (!_isActive) return;
        if (_activeId != id) return;

        StopCountdownCoroutine();
        SuccessSabotage();
    }

    [PunRPC]
    private void RPC_FailSabotage(int idRaw)
    {
        var id = (SabotageId)idRaw;
        if (!_isActive) return;
        if (_activeId != id) return;

        StopCountdownCoroutine();
        FailSabotage();
    }

    #region Coroutine
    private void StartCountdownCoroutine()
    {
        StopCountdownCoroutine();
        _countdownCor = StartCoroutine(Co_countdown());
    }

    private void StopCountdownCoroutine()
    {
        if (_countdownCor != null)
        {
            StopCoroutine(_countdownCor);
            _countdownCor = null;
        }
    }

    private IEnumerator Co_countdown()
    {
        SetCountdownUI(true);

        var delay = new WaitForSeconds(1f);
        while (_isActive)
        {
            double elapsed = PhotonNetwork.Time - _startServerTime;
            _remainingTime = Mathf.Max(0f, _activeDuration - (float)elapsed);

            UpdateCountdownUI(_remainingTime);

            if (_remainingTime <= 0f) break;

            yield return delay;
        }

        // 코루틴을 빠져나오면 실패 (판정은 Master만)
        if(_isActive && PhotonNetwork.IsMasterClient)
        {
            _pv.RPC(nameof(RPC_FailSabotage), RpcTarget.All, (int)_activeId);
        }
    }

    private void SetCountdownUI(bool on)
    {
        if (_countdownPanel != null)
            _countdownPanel.SetActive(on);
    }

    private void UpdateCountdownUI(float time)
    {
        if (_countdownText == null) return;

        if (time < 0f) time = 0f;

        int total = Mathf.CeilToInt(time);
        int m = total / 60;
        int s = total % 60;
        _countdownText.text = $"{m:00} : {s:00}";
    }

    private void OnDisable()
    {
        StopCountdownCoroutine();
        SetCountdownUI(false);
    }

    private void OnDestroy()
    {
        StopCountdownCoroutine();
    }
    #endregion

    private void SuccessSabotage()
    {
        SabotageId resolved = _activeId;

        ResetState();
        OnSabotageResolved(resolved);
    }

    private void FailSabotage()
    {
        SabotageId failed = _activeId;

        ResetState();
        OnSabotageFailed(failed);
    }

    private void ResetState()
    {
        StopCountdownCoroutine();

        _isActive = false;
        _activeId = SabotageId.None;
        _remainingTime = 0f;
        _activeDuration = 0f;
        _startServerTime = 0;

        SetCountdownUI(false);
    }

    private void OnSabotageStart(SabotageId id)
    {
        Debug.Log($"[Sabotage] Start : {id}");

        if(StatusNoticeUI.Instance != null)
        {
            if (id == SabotageId.Engine)
                StatusNoticeUI.Instance.ShowMessage("사보타지 발생!", "엔진실을 수리하세요!");
            else
                StatusNoticeUI.Instance.ShowMessage("사보타지 발생!", id.ToString());
        }
    }

    // Light 전용 RPC (타이머/승패 없음)
    [PunRPC]
    private void RPC_TriggerLight(bool on)
    {
        Debug.Log($"[Sabotage] Light : {(on ? "ON" : "OFF")}");

        if (StatusNoticeUI.Instance != null)
        {
            StatusNoticeUI.Instance.ShowMessage(
                on ? "정전 발생!" : "전력 복구!",
                on ? "시야가 제한됩니다." : ""
            );
        }

        if (_blackoutBinder != null)
            _blackoutBinder.RequestBlackout(on);
    }

    private void OnSabotageResolved(SabotageId id)
    {
        Debug.Log($"[Sabotage] Resolved : {id}");

        if (StatusNoticeUI.Instance != null)
            StatusNoticeUI.Instance.ShowMessage("사보타지 해제!", id.ToString());
    }

    private void OnSabotageFailed(SabotageId id) // RPC_FailSabotage(All)로 실행
    {
        Debug.Log($"[Sabotage] Failed : {id}");
        if (StatusNoticeUI.Instance != null)
            StatusNoticeUI.Instance.ShowMessage("사보타지 실패", "시민 패배");

        PlayerManager.Instance.NoticeGameOverToAllPlayers(false);
    }
}