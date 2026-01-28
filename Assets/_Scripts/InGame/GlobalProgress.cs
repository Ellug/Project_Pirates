using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 글로벌 진행도의 역할
// 1. 미션 클리어 소식을 받으면 해당 점수만큼 진행도를 증가 시킴.
// 2. 일정 시간마다 진행도를 1%씩 증가 시킴.
// 3. 진행도가 100%가 되면 시민 승리
public class GlobalProgress : MonoBehaviourPunCallbacks
{
    [Header("Reference")]
    [SerializeField] private CustomPropertyManager _roomProps;
    [SerializeField] private Image _progressBar;
    [SerializeField] private TextMeshProUGUI _progressPercent;

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
            _roomProps = FindFirstObjectByType<CustomPropertyManager>();
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

    public void CompleteMission(float missionScore)
    {
        string key = MissionPrefix + missionScore;

        if (_missionStates.TryGetValue(key, out bool done) && done)
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

    // 미션 완료 시 점수
    private void ApplyCompletedCount(int count, bool isInit)
    {
        _completedMissionCount = count;

        // int percent = GetProgressPercent();

        // OnProgressChanged?.Invoke(percent);

        // 진행도 100% 시 승리 알림.
        // if()
        //     PlayerManager.Instance.NoticeGameOverToAllPlayers(true);
    }

    public bool IsMissionCompleted(string missionId)
    {
        return _missionStates.TryGetValue(missionId, out bool done) && done;
    }

    public int GetCompletedCount() => _completedMissionCount;

    public float GetProgressPercent()
    {
        return 0f;
    }
}
