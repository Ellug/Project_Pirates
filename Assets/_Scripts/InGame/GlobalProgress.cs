using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using UnityEngine;

public class GlobalProgress : MonoBehaviourPunCallbacks
{
    [Header("Reference")]
    [SerializeField] private CustumPropertieManager _roomProps;

    [Header("Mission Setting")]
    [Tooltip("전체 미션 갯수"), SerializeField] private int _totalMissionCount = 5;

    // 커스텀 프로퍼티 키
    private const string Mission1Key = "world.mission1";
    private const string CompletedCountKey = "world.mission.completedCount";

    // 캐시
    private bool _isMission1Completed;
    private int _completedMissionCount;

    // 상태 변경
    public event Action<bool> OnMission1StateChanged;


    // 전채 진행도 변경
    public event Action<int> OnProgressChanged;

    private void Awake()
    {
        if (_roomProps == null)
            _roomProps = FindFirstObjectByType<CustumPropertieManager>();
    }

    private void Start()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        _roomProps.OnRoomPropertyChanged += OnRoomPropertyChanged;

        // ===== 미션1 상태 초기화 =====
        if (_roomProps.TryGet(Mission1Key, out bool completed))
            ApplyMission1(completed, true);
        else
        {
            if (PhotonNetwork.IsMasterClient)
                _roomProps.Set(Mission1Key, false);

            ApplyMission1(false, true);
        }

        // ===== 완료된 미션 수 초기화 =====
        if (_roomProps.TryGet(CompletedCountKey, out int count))
            ApplyCompletedCount(count, true);
        else
        {
            if (PhotonNetwork.IsMasterClient)
                _roomProps.Set(CompletedCountKey, 0);

            ApplyCompletedCount(0, true);
        }
    }

    private void OnDestroy()
    {
        if (_roomProps != null)
            _roomProps.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    // ========================================================================
    // 외부(미션)에서 호출하는 영역
    // ========================================================================

    public void CompleteMission1()
    {
        if (_isMission1Completed)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        _roomProps.Set(Mission1Key, true);

        // 미션 하나 완료 → 전체 완료 수 증가
        _roomProps.Set(CompletedCountKey, _completedMissionCount + 1);
    }

    // ========================================================================
    // RoomProperty 콜백
    // ========================================================================

    private void OnRoomPropertyChanged(Hashtable changedProps)
    {
        if (changedProps.TryGetValue(Mission1Key, out var m1))
            ApplyMission1((bool)m1, false);

        if (changedProps.TryGetValue(CompletedCountKey, out var count))
            ApplyCompletedCount((int)count, false);
    }

    // ========================================================================
    // Apply (로컬 반영)
    // ========================================================================

    private void ApplyMission1(bool completed, bool isInit)
    {
        _isMission1Completed = completed;

        Debug.Log(isInit
            ? $"[GlobalProgress] Mission1 Init : {_isMission1Completed}"
            : $"[GlobalProgress] Mission1 Changed : {_isMission1Completed}");

        OnMission1StateChanged?.Invoke(_isMission1Completed);
    }

    private void ApplyCompletedCount(int count, bool isInit)
    {
        _completedMissionCount = count;

        int percent = GetProgressPercent();

        Debug.Log(isInit
            ? $"[GlobalProgress] Progress Init : {_completedMissionCount}/{_totalMissionCount} ({percent}%)"
            : $"[GlobalProgress] Progress Changed : {_completedMissionCount}/{_totalMissionCount} ({percent}%)");

        OnProgressChanged?.Invoke(percent);
    }

    // ========================================================================
    // 외부 조회용
    // ========================================================================

    public bool IsMission1Completed()
    {
        return _isMission1Completed;
    }

    public int GetCompletedCount()
    {
        return _completedMissionCount;
    }

    public int GetProgressPercent()
    {
        if (_totalMissionCount <= 0)
            return 0;

        return Mathf.Clamp(
            Mathf.RoundToInt((float)_completedMissionCount / _totalMissionCount * 100f),
            0,
            100);
    }
}
