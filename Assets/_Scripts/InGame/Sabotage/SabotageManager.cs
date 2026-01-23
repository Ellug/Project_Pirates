using TMPro;
using System;
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
    Power,
    Light
}

public class SabotageManager : MonoBehaviour
{
    [Header("State")]
    public bool IsActive => _isActive;
    public SabotageId ActiveSabotage => _activeId;
    public float RemainingTime => _remainingTime;

    [Header("Config")]
    [SerializeField] private float _defaultDuration = 60f;

    [Header("Countdown UI")]
    [SerializeField] private GameObject _countdownPanel;
    [SerializeField] private TMP_Text _countdownText;

    private bool _isActive;
    private SabotageId _activeId = SabotageId.None;
    private float _remainingTime;

    private Coroutine _countdownCor;

    private void Awake()
    {
        SetCountdownUI(false);
    }

    public bool TriggerSabotage(SabotageId id, float duration = -1f)
    {
        if (_isActive) return false;
        if (id == SabotageId.None) return false;

        _isActive = true;
        _activeId = id;
        _remainingTime = duration > 0f ? duration : _defaultDuration;

        OnSabotageStart(id);
        StartCountdownCoroutine();
        return true;
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
        UpdateCountdownUI(_remainingTime);

        // waitforsec으로 카운트다운
        while (_isActive && _remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;
            UpdateCountdownUI(_remainingTime);
            yield return null;
        }

        // 코루틴을 빠져나오면 실패
        if(_isActive)
        {
            FailSabotage();
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
        int s = total & 60;
        _countdownText.text = $"{m:00} : {s:00}";
    }
    #endregion

    public bool ResolveSabotage(SabotageId id)
    {
        if (!_isActive) return false;
        if (_activeId != id) return false;

        // 시민이 해결하면 코루틴 중단
        StopCountdownCoroutine();
        SuccessSabotage();
        return true;
    }

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
        _isActive = false;
        _activeId = SabotageId.None;
        _remainingTime = 0f;
    }

    private void OnSabotageStart(SabotageId id)
    {
        Debug.Log($"[Sabotage] Start : {id}");
        // TODO :
        // 카운트다운 UI 표시
        // 월드 효과 적용 ( 조명 끄기, 엔진 정지 등 )
    }

    // UI 연결용
    private void OnSabotageResolved(SabotageId id)
    {
        Debug.Log($"[Sabotage] Resolved : {id}");
        // TODO :
        // UI 종료
        // 월드 효과 원복시키기
    }

    private void OnSabotageFailed(SabotageId id)
    {
        Debug.Log($"[Sabotage] Failed : {id}");
        // TODO :
        // 마피아 승리 처리
        // 게임 종료 트리거
    }
}