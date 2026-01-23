using System;
using UnityEngine;

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

    private bool _isActive;
    private SabotageId _activeId = SabotageId.None;
    private float _remainingTime;

    private void Update()
    {
        if (!_isActive) return;

        _remainingTime -= Time.deltaTime;

        if(_remainingTime <= 0f)
        {
            FailSabotage();
        }
    }

    public bool TriggerSabotage(SabotageId id, float duration = -1f)
    {
        if (_isActive) return false;
        if (id == SabotageId.None) return false;

        _isActive = true;
        _activeId = id;
        _remainingTime = duration > 0f ? duration : _defaultDuration;

        OnSabotageStart(id);
        return true;
    }

    public bool ResolveSabotage(SabotageId id)
    {
        if (!_isActive) return false;
        if (_activeId != id) return false;

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