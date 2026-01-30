using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

// 투표 관련 Room/Player Custom Properties 관리
// 플레이어 리스트, 사망자 상태, 투표 현황을 동기화
// PhotonView 없이 Custom Properties만 사용
public class VoteRoomProperties : MonoBehaviourPunCallbacks
{
    public static VoteRoomProperties Instance { get; private set; }

    // Room Property Keys
    private const string KEY_VOTE_PHASE = "VotePhase";
    private const string KEY_PLAYER_LIST = "VotePlayerList";      // int[] ActorNumbers
    private const string KEY_DEAD_PLAYERS = "DeadPlayers";        // int[] 사망한 ActorNumbers

    // Player Property Keys (각 플레이어가 자신의 투표를 저장)
    private const string KEY_MY_VOTE = "MyVote";                  // int 내가 투표한 대상 ActorNumber

    // 이벤트
    public event Action<VotePhase> OnVotePhaseChanged;
    public event Action<List<VotePlayerInfo>> OnPlayerListUpdated;
    public event Action<int, int> OnVoteReceived;  // voterActorNum, targetActorNum

    // 로컬 캐시
    private List<VotePlayerInfo> _playerInfoList = new();
    private HashSet<int> _deadPlayers = new();
    private VotePhase _currentPhase = VotePhase.None;

    public VotePhase CurrentPhase => _currentPhase;
    public IReadOnlyList<VotePlayerInfo> PlayerList => _playerInfoList;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this); // 컴포넌트만 제거 (게임오브젝트는 유지)
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
            InitializePlayerList();
    }

    // 방에 있는 모든 플레이어로 리스트 초기화 (마스터만 호출)
    public void InitializePlayerList()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _playerInfoList.Clear();
        _deadPlayers.Clear();
        int[] actorNumbers = new int[PhotonNetwork.CurrentRoom.PlayerCount];
        int index = 0;

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            actorNumbers[index++] = player.ActorNumber;
            _playerInfoList.Add(new VotePlayerInfo(player.ActorNumber, player.NickName));
        }

        // Room Property에 저장 (사망자 목록 초기화 포함)
        var props = new Hashtable
        {
            { KEY_PLAYER_LIST, actorNumbers },
            { KEY_DEAD_PLAYERS, new int[0] },
            { KEY_VOTE_PHASE, (int)VotePhase.None }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // 모든 플레이어의 MyVote 초기화
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            var voteProps = new Hashtable { { KEY_MY_VOTE, -1 } };
            player.SetCustomProperties(voteProps);
        }

        Debug.Log("[VoteRoomProperties] 투표 시스템 초기화 완료");
    }

    // 플레이어 사망 등록
    public void MarkPlayerDead(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _deadPlayers.Add(actorNumber);
        int[] deadArray = new int[_deadPlayers.Count];
        _deadPlayers.CopyTo(deadArray);

        var props = new Hashtable { { KEY_DEAD_PLAYERS, deadArray } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 투표 단계 변경 (마스터만)
    public void SetVotePhase(VotePhase phase)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var props = new Hashtable { { KEY_VOTE_PHASE, (int)phase } };

        // 투표 시작 시 모든 플레이어의 투표 데이터 초기화
        if (phase == VotePhase.Discussion)
        {
            // 모든 플레이어의 MyVote를 -1로 초기화
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                var voteProps = new Hashtable { { KEY_MY_VOTE, -1 } };
                player.SetCustomProperties(voteProps);
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 투표 제출 (로컬 플레이어가 호출) - RPC 없이 Player Custom Properties 사용
    public void SubmitVote(int targetActorNumber)
    {
        // 죽은 플레이어는 투표 불가
        if (IsPlayerDead(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            Debug.Log("[VoteRoomProperties] 죽은 플레이어는 투표할 수 없음");
            return;
        }

        // 자신의 Custom Properties에 투표 저장
        var props = new Hashtable { { KEY_MY_VOTE, targetActorNumber } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[VoteRoomProperties] 투표 제출: {targetActorNumber}");
    }

    // Room Property 변경 콜백
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 투표 단계 변경
        if (propertiesThatChanged.TryGetValue(KEY_VOTE_PHASE, out var phaseObj))
        {
            VotePhase newPhase = (VotePhase)(int)phaseObj;

            // 실제로 Phase가 변경되었을 때만 이벤트 발생
            if (_currentPhase != newPhase)
            {
                _currentPhase = newPhase;
                OnVotePhaseChanged?.Invoke(_currentPhase);

                // 단계가 바뀔 때 플레이어 리스트도 갱신
                if (_currentPhase == VotePhase.Discussion)
                {
                    RebuildPlayerInfoList();
                }
            }
        }

        // 플레이어 리스트 변경
        if (propertiesThatChanged.TryGetValue(KEY_PLAYER_LIST, out var listObj))
        {
            RebuildPlayerInfoList();
        }

        // 사망자 변경
        if (propertiesThatChanged.TryGetValue(KEY_DEAD_PLAYERS, out var deadObj))
        {
            UpdateDeadPlayers((int[])deadObj);
            OnPlayerListUpdated?.Invoke(_playerInfoList);
        }
    }

    // Player Property 변경 콜백 (다른 플레이어가 투표했을 때)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.TryGetValue(KEY_MY_VOTE, out var voteObj))
        {
            int targetActorNumber = (int)voteObj;

            // 투표 이벤트 발생
            OnVoteReceived?.Invoke(targetPlayer.ActorNumber, targetActorNumber);

            // 플레이어 리스트의 투표 정보 업데이트
            UpdateAllVoteData();
            OnPlayerListUpdated?.Invoke(_playerInfoList);

            Debug.Log($"[VoteRoomProperties] {targetPlayer.NickName}이 {targetActorNumber}에게 투표함");
        }
    }

    private void RebuildPlayerInfoList()
    {
        int[] actorNumbers = GetActorNumbersFromProperty();
        if (actorNumbers == null) return;

        _playerInfoList.Clear();
        foreach (int actorNum in actorNumbers)
        {
            string nickName = "Unknown";
            if (PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNum, out var player))
                nickName = player.NickName;

            var info = new VotePlayerInfo(actorNum, nickName)
            {
                IsDead = _deadPlayers.Contains(actorNum)
            };
            _playerInfoList.Add(info);
        }

        // 현재 투표 상태도 반영
        UpdateAllVoteData();

        OnPlayerListUpdated?.Invoke(_playerInfoList);
    }

    private void UpdateDeadPlayers(int[] deadArray)
    {
        _deadPlayers.Clear();
        foreach (int actorNum in deadArray)
            _deadPlayers.Add(actorNum);

        // 플레이어 리스트의 사망 상태 업데이트
        foreach (var info in _playerInfoList)
            info.IsDead = _deadPlayers.Contains(info.ActorNumber);
    }

    // 모든 플레이어의 투표 데이터를 Player Custom Properties에서 읽어와 업데이트
    private void UpdateAllVoteData()
    {
        // 먼저 모든 투표 수 초기화
        foreach (var info in _playerInfoList)
        {
            info.VoteCount = 0;
            info.VotedFor = -1;
        }

        // 각 플레이어의 Custom Properties에서 투표 정보 읽기
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.CustomProperties.TryGetValue(KEY_MY_VOTE, out var voteObj))
            {
                int votedFor = (int)voteObj;

                // 투표자의 VotedFor 업데이트
                var voter = _playerInfoList.Find(p => p.ActorNumber == player.ActorNumber);
                if (voter != null)
                    voter.VotedFor = votedFor;

                // 대상의 VoteCount 증가 (스킵(-2)이나 미투표(-1)가 아닌 경우)
                if (votedFor >= 0)
                {
                    var target = _playerInfoList.Find(p => p.ActorNumber == votedFor);
                    if (target != null)
                        target.VoteCount++;
                }
            }
        }
    }

    private int[] GetActorNumbersFromProperty()
    {
        if (PhotonNetwork.CurrentRoom?.CustomProperties?.TryGetValue(KEY_PLAYER_LIST, out var obj) == true)
            return (int[])obj;
        return null;
    }

    // 플레이어가 사망했는지 확인
    public bool IsPlayerDead(int actorNumber)
    {
        return _deadPlayers.Contains(actorNumber);
    }

    // 생존자 수 반환
    public int GetAlivePlayerCount()
    {
        return _playerInfoList.Count - _deadPlayers.Count;
    }

    // 모든 생존자가 투표했는지 확인
    public bool AllAlivePlayersVoted()
    {
        foreach (var info in _playerInfoList)
        {
            if (!info.IsDead && info.VotedFor == -1)
                return false;
        }
        return true;
    }

    // 투표 결과 계산 (가장 많은 표를 받은 플레이어)
    public VotePlayerInfo GetVoteResult()
    {
        // 결과 계산 전에 최신 투표 데이터 반영
        UpdateAllVoteData();

        // 스킵 투표 수 계산
        int skipVoteCount = 0;
        foreach (var info in _playerInfoList)
        {
            if (info.VotedFor == -2) // 스킵
                skipVoteCount++;
        }

        VotePlayerInfo topVoted = null;
        int maxVotes = 0;
        bool isTie = false;

        foreach (var info in _playerInfoList)
        {
            if (info.VoteCount > maxVotes)
            {
                maxVotes = info.VoteCount;
                topVoted = info;
                isTie = false;
            }
            else if (info.VoteCount == maxVotes && maxVotes > 0)
            {
                isTie = true;
            }
        }

        // 스킵이 최다득표보다 많거나 같으면 아무도 처형 안 됨
        if (skipVoteCount >= maxVotes)
            return null;

        // 동점이거나 0표면 null 반환 (아무도 처형 안 됨)
        return isTie ? null : topVoted;
    }
}
