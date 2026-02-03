using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 투표 UI 전체 관리
// 플레이어 슬롯 리스트, 타이머, 단계 표시 등
public class VoteUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private GameObject _playerSlotPrefab;
    [SerializeField] private TextMeshProUGUI _phaseText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _skipButton;

    [Header("Phase Texts")]
    [SerializeField] private string _discussionText = "토론 시간";
    [SerializeField] private string _votingText = "투표 시간";
    [SerializeField] private string _resultText = "투표 결과";

    private List<VotePlayerSlot> _playerSlots = new();
    private VotePhase _currentPhase = VotePhase.None;
    private bool _hasVoted = false;
    private int _reporterActorNumber = -1;  // 신고자 ActorNumber

    void Awake()
    {
        if (_skipButton != null)
            _skipButton.onClick.AddListener(OnSkipButtonClicked);
    }

    void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(OnSkipButtonClicked);
    }

    // 투표 UI 초기화 (투표 시작 시 호출)
    public void ResetUI()
    {
        _hasVoted = false;
        _reporterActorNumber = -1;

        // 기존 슬롯 모두 제거
        foreach (var slot in _playerSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        _playerSlots.Clear();

        UpdatePhaseUI();
    }

    // 신고자 설정
    public void SetReporter(int actorNumber)
    {
        _reporterActorNumber = actorNumber;

        // 기존 슬롯에 신고자 마크 업데이트
        foreach (var slot in _playerSlots)
        {
            if (slot != null)
                slot.SetReporter(slot.ActorNumber == _reporterActorNumber);
        }
    }

    // 투표 단계 변경 처리
    public void OnPhaseChanged(VotePhase phase)
    {
        _currentPhase = phase;

        // 토론/투표 시작 시 투표 상태 초기화
        if (phase == VotePhase.Discussion || phase == VotePhase.Voting || phase == VotePhase.None)
            _hasVoted = false;

        UpdatePhaseUI();
        UpdateSlotInteractivity();
    }

    private void UpdatePhaseUI()
    {
        if (_phaseText != null)
        {
            _phaseText.text = _currentPhase switch
            {
                VotePhase.Discussion => _discussionText,
                VotePhase.Voting => _votingText,
                VotePhase.Result => _resultText,
                _ => ""
            };
        }
    }

    // 플레이어 리스트 업데이트
    public void UpdatePlayerList(List<VotePlayerInfo> playerList)
    {
        // 기존 슬롯 제거
        foreach (var slot in _playerSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        _playerSlots.Clear();

        if (_playerListContainer == null || _playerSlotPrefab == null)
        {
            Debug.LogWarning("[VoteUI] PlayerListContainer 또는 PlayerSlotPrefab이 설정되지 않음");
            return;
        }

        // 새 슬롯 생성
        foreach (var playerInfo in playerList)
        {
            GameObject slotObj = Instantiate(_playerSlotPrefab, _playerListContainer);
            VotePlayerSlot slot = slotObj.GetComponent<VotePlayerSlot>();

            if (slot != null)
            {
                bool isReporter = playerInfo.ActorNumber == _reporterActorNumber;
                slot.Initialize(playerInfo, OnPlayerSlotClicked, isReporter);
                _playerSlots.Add(slot);
            }
        }

        UpdateSlotInteractivity();
    }

    private void UpdateSlotInteractivity()
    {
        // 죽은 플레이어는 투표 불가
        bool isLocalPlayerDead = VoteRoomProperties.Instance != null &&
            VoteRoomProperties.Instance.IsPlayerDead(PhotonNetwork.LocalPlayer.ActorNumber);

        bool canVote = _currentPhase == VotePhase.Voting && !_hasVoted && !isLocalPlayerDead;
        bool canSkip = (_currentPhase == VotePhase.Voting || _currentPhase == VotePhase.Discussion) && !_hasVoted && !isLocalPlayerDead;

        foreach (var slot in _playerSlots)
        {
            if (slot != null)
                slot.SetInteractable(canVote);
        }

        // 스킵 버튼도 죽은 플레이어는 비활성화
        if (_skipButton != null)
            _skipButton.interactable = canSkip;
    }

    // 플레이어 슬롯 클릭 (투표)
    private void OnPlayerSlotClicked(int actorNumber)
    {
        if (_currentPhase != VotePhase.Voting) return;
        if (_hasVoted) return;

        // 죽은 플레이어는 투표 불가
        if (VoteRoomProperties.Instance != null &&
            VoteRoomProperties.Instance.IsPlayerDead(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            Debug.Log("[VoteUI] 죽은 플레이어는 투표할 수 없음");
            return;
        }

        _hasVoted = true;

        // VoteManager에 투표 전달
        if (VoteManager.Instance != null)
            VoteManager.Instance.SubmitVote(actorNumber);

        UpdateSlotInteractivity();
        UpdatePhaseUI();

        Debug.Log($"[VoteUI] {actorNumber}에게 투표함");
    }

    // 스킵 버튼 클릭
    private void OnSkipButtonClicked()
    {
        if (_currentPhase != VotePhase.Voting && _currentPhase != VotePhase.Discussion) return;
        if (_hasVoted) return;

        // 죽은 플레이어는 투표 불가
        if (VoteRoomProperties.Instance != null &&
            VoteRoomProperties.Instance.IsPlayerDead(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            Debug.Log("[VoteUI] 죽은 플레이어는 스킵할 수 없음");
            return;
        }

        _hasVoted = true;

        if (VoteManager.Instance != null)
            VoteManager.Instance.SubmitSkipVote();

        UpdateSlotInteractivity();
        UpdatePhaseUI();

        Debug.Log(_currentPhase == VotePhase.Discussion ? "[VoteUI] 토론 스킵" : "[VoteUI] 투표 스킵");
    }

    // 타이머 텍스트 업데이트 (외부에서 호출)
    public void UpdateTimer(float remainingTime)
    {
        if (_timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
            _timerText.text = seconds.ToString();
        }
    }
}
