using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PhotonView 없이 동작하는 VoteManager
// Room/Player Custom Properties만 사용하여 동기화
public class VoteManager : MonoBehaviourPunCallbacks
{
    public static VoteManager Instance { get; private set; }

    [Header("Alert Panels")]
    [SerializeField] private GameObject _DeadBodyAlertPanel;
    [SerializeField] private GameObject _CenterAlertPanel;
    [SerializeField] private GameObject _votePanel;

    [Header("Vote UI")]
    [SerializeField] private VoteUI _voteUI;

    [Header("Teleport Area")]
    [SerializeField] private Transform _teleportArea;
    [SerializeField] private float _areaRangeX = 5f;
    [SerializeField] private float _areaRangeZ = 5f;
    [SerializeField] private float _minDistance = 1.5f;

    [Header("Timing")]
    [SerializeField] private float _teleportDelay = 1f;
    [SerializeField] private float _panelFadeOutDelay = 3f;
    [SerializeField] private float _discussionTime = 30f;
    [SerializeField] private float _votingTime = 20f;
    [SerializeField] private float _resultDisplayTime = 5f;
    [SerializeField] private float _postVoteCleanupDelay = 2f;

    [Header("Dead Player Area")]
    [SerializeField] private Transform _deadPlayerArea;

    private List<Vector3> _usedPositions = new();
    private VoteRoomProperties _voteProps;
    private Coroutine _voteCoroutine;
    private Coroutine _timerCoroutine;
    private Coroutine _postVoteCleanupCoroutine;
    private bool _voteActive;
    private int _currentReporterActorNumber = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // VoteRoomProperties 찾기 또는 생성
        _voteProps = FindFirstObjectByType<VoteRoomProperties>();

        if (_voteProps != null)
        {
            _voteProps.OnVotePhaseChanged += HandleVotePhaseChanged;
            _voteProps.OnPlayerListUpdated += HandlePlayerListUpdated;
        }

        // 투표 UI 초기 비활성화
        if (_votePanel != null)
            _votePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (_voteProps != null)
        {
            _voteProps.OnVotePhaseChanged -= HandleVotePhaseChanged;
            _voteProps.OnPlayerListUpdated -= HandlePlayerListUpdated;
        }
    }

    // 시체 신고됨 -> RPC로 호출됨 (PlayerManager에서)
    public void OnDeadBodyReported(int reporterActorNumber)
    {
        _currentReporterActorNumber = reporterActorNumber;
        StartCoroutine(TeleportSequence(isDeadBody: true));
    }

    public void OnCenterReported(int reporterActorNumber)
    {
        _currentReporterActorNumber = reporterActorNumber;
        StartCoroutine(TeleportSequence(isDeadBody: false));
    }

    private IEnumerator TeleportSequence(bool isDeadBody)
    {
        // 알림 패널 표시
        if (isDeadBody)
            _DeadBodyAlertPanel.SetActive(true);
        else
            _CenterAlertPanel.SetActive(true);

        Debug.Log("[VoteManager] 시체 발견! 알림 패널 표시");

        // 1초 대기
        yield return new WaitForSeconds(_teleportDelay);

        // 로컬 플레이어 텔레포트
        TeleportLocalPlayer();

        // 2초 뒤 패널 비활성화
        yield return new WaitForSeconds(_panelFadeOutDelay);

        HideAllAlertPanel();

        // 사용된 위치 초기화
        _usedPositions.Clear();

        // 마스터 클라이언트만 투표 시작
        if (PhotonNetwork.IsMasterClient)
            StartVoteSequence();
    }

    // 투표 시퀀스 시작 (마스터만 호출)
    private void StartVoteSequence()
    {
        if (_voteCoroutine != null)
            StopCoroutine(_voteCoroutine);

        _voteCoroutine = StartCoroutine(VoteSequenceCoroutine());
    }

    private IEnumerator VoteSequenceCoroutine()
    {
        // 1. 토론 시간
        _voteProps.SetVotePhase(VotePhase.Discussion);
        yield return new WaitForSeconds(_discussionTime);

        // 2. 투표 시간
        _voteProps.SetVotePhase(VotePhase.Voting);

        float elapsed = 0f;
        while (elapsed < _votingTime)
        {
            // 모든 생존자가 투표하면 조기 종료
            if (_voteProps.AllAlivePlayersVoted())
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 결과 표시
        _voteProps.SetVotePhase(VotePhase.Result);
        yield return new WaitForSeconds(_resultDisplayTime);

        // 4. 투표 결과 처리
        ProcessVoteResult();

        // 5. 투표 종료
        _voteProps.SetVotePhase(VotePhase.None);
    }

    private void ProcessVoteResult()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_voteProps == null) return;

        var result = _voteProps.GetVoteResult();
        if (result != null && result.VoteCount > 0)
        {
            Debug.Log($"[VoteManager] {result.NickName}이(가) 처형되었습니다. (득표: {result.VoteCount})");
            // 처형된 플레이어 사망 처리
            _voteProps.MarkPlayerDead(result.ActorNumber);
            ExecutePlayerByVote(result.ActorNumber);
        }
        else
        {
            Debug.Log("[VoteManager] 아무도 처형되지 않았습니다.");
        }
    }

    private void ExecutePlayerByVote(int actorNumber)
    {
        var target = FindPlayerControllerByActorNumber(actorNumber);
        if (target == null)
        {
            Debug.LogWarning($"[VoteManager] 처형 대상 플레이어를 찾지 못함: ActorNumber={actorNumber}");
            return;
        }

        var model = target.GetComponent<PlayerModel>();
        if (model != null && model.IsDead)
        {
            Debug.Log($"[VoteManager] 이미 사망한 플레이어: ActorNumber={actorNumber}");
            return;
        }

        target.photonView.RPC(nameof(PlayerController.RpcExecuteByVote), target.photonView.Owner);
    }

    private PlayerController FindPlayerControllerByActorNumber(int actorNumber)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            var view = player.GetComponent<PhotonView>();
            if (view != null && view.OwnerActorNr == actorNumber)
                return player;
        }

        return null;
    }

    private void HandleVotePhaseChanged(VotePhase phase)
    {
        Debug.Log($"[VoteManager] 투표 단계 변경: {phase}");

        switch (phase)
        {
            case VotePhase.None:
                if (_votePanel != null)
                    _votePanel.SetActive(false);
                if (InputManager.Instance != null)
                    InputManager.Instance.SetUIMode(false);
                break;

            case VotePhase.Discussion:
            case VotePhase.Voting:
            case VotePhase.Result:
                if (_votePanel != null)
                    _votePanel.SetActive(true);
                if (InputManager.Instance != null)
                    InputManager.Instance.SetUIMode(true);
                break;
        }

        // VoteUI에 단계 전달
        if (_voteUI != null)
            _voteUI.OnPhaseChanged(phase);

        StartPhaseTimer(phase);

        if (phase == VotePhase.Discussion)
        {
            if (_postVoteCleanupCoroutine != null)
            {
                StopCoroutine(_postVoteCleanupCoroutine);
                _postVoteCleanupCoroutine = null;
            }

            _voteActive = true;
            if (_voteUI != null)
            {
                _voteUI.ResetUI();
                _voteUI.SetReporter(_currentReporterActorNumber);
            }
        }

        if (phase == VotePhase.None && _voteActive)
        {
            _voteActive = false;
            StartPostVoteCleanup();
        }
    }

    private void HandlePlayerListUpdated(List<VotePlayerInfo> playerList)
    {
        if (_voteUI != null)
            _voteUI.UpdatePlayerList(playerList);
    }

    // 로컬 플레이어가 투표 제출
    public void SubmitVote(int targetActorNumber)
    {
        if (_voteProps == null) return;
        if (_voteProps.CurrentPhase != VotePhase.Voting) return;

        _voteProps.SubmitVote(targetActorNumber);
    }

    // 스킵 투표
    public void SubmitSkipVote()
    {
        SubmitVote(-2); // -2 = 스킵
    }

    private void TeleportLocalPlayer()
    {
        if (_teleportArea == null)
        {
            Debug.LogWarning("[VoteManager] 텔레포트 영역이 어디감?");
            return;
        }

        GameObject localPlayer = PlayerController.LocalInstancePlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[VoteManager] 로컬 플레이어 어디감?");
            return;
        }

        // 죽은 플레이어는 텔레포트하지 않음
        PlayerModel playerModel = localPlayer.GetComponent<PlayerModel>();
        if (playerModel != null && playerModel.IsDead)
        {
            Debug.Log("[VoteManager] 죽은 플레이어는 텔레포트하지 않음");
            return;
        }

        Vector3 randomPos = GetRandomNonOverlappingPosition();
        localPlayer.transform.position = randomPos;

        Debug.Log($"[VoteManager] 플레이어 텔레포트 완료: {randomPos}");
    }

    private void StartPhaseTimer(VotePhase phase)
    {
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        switch (phase)
        {
            case VotePhase.Discussion:
                _timerCoroutine = StartCoroutine(PhaseTimerCoroutine(_discussionTime));
                break;
            case VotePhase.Voting:
                _timerCoroutine = StartCoroutine(PhaseTimerCoroutine(_votingTime));
                break;
            case VotePhase.Result:
                _timerCoroutine = StartCoroutine(PhaseTimerCoroutine(_resultDisplayTime));
                break;
            default:
                if (_voteUI != null)
                    _voteUI.UpdateTimer(0f);
                break;
        }
    }

    private IEnumerator PhaseTimerCoroutine(float duration)
    {
        float remaining = duration;

        while (remaining > 0f)
        {
            if (_voteUI != null)
                _voteUI.UpdateTimer(remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (_voteUI != null)
            _voteUI.UpdateTimer(0f);
    }

    private void StartPostVoteCleanup()
    {
        if (_postVoteCleanupCoroutine != null)
            StopCoroutine(_postVoteCleanupCoroutine);

        _postVoteCleanupCoroutine = StartCoroutine(PostVoteCleanupCoroutine());
    }

    private IEnumerator PostVoteCleanupCoroutine()
    {
        if (_postVoteCleanupDelay > 0f)
            yield return new WaitForSeconds(_postVoteCleanupDelay);

        if (PhotonNetwork.IsMasterClient && PlayerManager.Instance != null)
            PlayerManager.Instance.RequestRemoveAllDeadBodies();

        MoveLocalDeadPlayerToArea();
    }

    private void MoveLocalDeadPlayerToArea()
    {
        if (_deadPlayerArea == null)
            return;

        GameObject localPlayer = PlayerController.LocalInstancePlayer;
        if (localPlayer == null)
            return;

        PlayerModel model = localPlayer.GetComponent<PlayerModel>();
        if (model == null || !model.IsDead)
            return;

        Vector3 targetPos = _deadPlayerArea.position;
        targetPos.y += 1.5f;
        localPlayer.transform.position = targetPos;
    }

    // 겹치지 않은 랜덤 위치 선정
    private Vector3 GetRandomNonOverlappingPosition()
    {
        if (_teleportArea == null)
            return Vector3.zero;

        Vector3 areaCenter = _teleportArea.position;

        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(-_areaRangeX, _areaRangeX);
            float randomZ = Random.Range(-_areaRangeZ, _areaRangeZ);
            Vector3 candidate = areaCenter + new Vector3(randomX, 0f, randomZ);

            bool isOverlapping = false;
            foreach (Vector3 usedPos in _usedPositions)
            {
                if (Vector3.Distance(candidate, usedPos) < _minDistance)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                _usedPositions.Add(candidate);
                return candidate;
            }
        }

        // 모든 시도 실패 시 랜덤 위치 반환
        Vector3 fallback = areaCenter + new Vector3(
            Random.Range(-_areaRangeX, _areaRangeX),
            0f,
            Random.Range(-_areaRangeZ, _areaRangeZ)
        );
        _usedPositions.Add(fallback);
        return fallback;
    }

    public void HideAllAlertPanel()
    {
        _DeadBodyAlertPanel.SetActive(false);
        _CenterAlertPanel.SetActive(false);
    }
}
