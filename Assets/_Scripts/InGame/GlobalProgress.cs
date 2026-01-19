using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GlobalProgress : MonoBehaviourPunCallbacks
{
    [Header("Reference")]
    [SerializeField] private CustumPropertieManager _roomProps;

    [Header("Mission Setting")]
    [Tooltip("전체 미션 갯수")]
    [SerializeField] private int _totalMissionCount = 5;

    // 공통 키
    private const string MissionPrefix = "world.mission.";
    private const string CompletedCountKey = "world.mission.completedCount";

    // 캐시
    private readonly Dictionary<string, bool> _missionStates = new();
    private int _completedMissionCount;

    // 이벤트
    public event Action<string, bool> OnMissionStateChanged;
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

        // 완료된 미션 수 초기화
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

    public void CompleteMission(string missionId)
    {
        string key = MissionPrefix + missionId;

        if (_missionStates.TryGetValue(key, out bool done) && done)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        _roomProps.Set(key, true);
        _roomProps.Set(CompletedCountKey, _completedMissionCount + 1);
    }
    private void OnRoomPropertyChanged(Hashtable changedProps)
    {
        foreach (var item in changedProps)
        {
            string key = item.Key as string;

            if (key == null)
                continue;

            if (key == CompletedCountKey)
            {
                ApplyCompletedCount((int)item.Value, false);
            }
            else if (key.StartsWith(MissionPrefix))
            {
                string missionId = key.Replace(MissionPrefix, "");
                ApplyMission(missionId, (bool)item.Value, false);
            }
        }
    }

    private void ApplyMission(string missionId, bool completed, bool isInit)
    {
        _missionStates[missionId] = completed;

        Debug.Log(isInit
            ? $"[GlobalProgress] {missionId} Init : {completed}"
            : $"[GlobalProgress] {missionId} Changed : {completed}");

        OnMissionStateChanged?.Invoke(missionId, completed);
    }

    private void ApplyCompletedCount(int count, bool isInit)
    {
        _completedMissionCount = count;

        int percent = GetProgressPercent();

        Debug.Log(isInit
            ? $"[GlobalProgress] Progress Init : {_completedMissionCount}/{_totalMissionCount} ({percent}%)"
            : $"[GlobalProgress] Progress Changed : {_completedMissionCount}/{_totalMissionCount} ({percent}%)");

        OnProgressChanged?.Invoke(percent);

        if(_completedMissionCount == _totalMissionCount)
            PlayerManager.Instance.NoticeGameOverToAllPlayers(true);
    }

    public bool IsMissionCompleted(string missionId)
    {
        return _missionStates.TryGetValue(missionId, out bool done) && done;
    }

    public int GetCompletedCount() => _completedMissionCount;

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
