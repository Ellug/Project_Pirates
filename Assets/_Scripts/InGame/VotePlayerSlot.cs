using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개별 플레이어 투표 슬롯 UI
// 플레이어 이름, 사망 표시, 득표 수, 투표 버튼
public class VotePlayerSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Transform _voteIconContainer;      // 득표 아이콘이 생성될 HorizontalLayoutGroup
    [SerializeField] private GameObject _voteIconPrefab;        // 득표 아이콘 프리팹
    [SerializeField] private Button _voteButton;
    [SerializeField] private Image _cardBackground;

    [Header("Status Marks")]
    [SerializeField] private GameObject _deadMark;              // 사망 마크 (사망 시 활성화)
    [SerializeField] private GameObject _aliveMark;             // 생존 마크 (생존 시 활성화)
    [SerializeField] private GameObject _reporterMark;          // 신고자 마크
    [SerializeField] private GameObject _votedMark;             // 투표 완료 마크 (이 플레이어가 투표했음)

    [Header("Color Avatar")]
    [SerializeField] private Image _colorAvatarImage;           // 플레이어 색상 표시 이미지

    [Header("Colors")]
    [SerializeField] private Color _aliveBackgroundColor = Color.white;
    [SerializeField] private Color _deadBackgroundColor = new(0.3f, 0.3f, 0.3f, 0.9f);

    [Header("Player Color Palette (enum 순서와 동일)")]
    [SerializeField] private Color[] _playerColors = new Color[]
    {
        new(1f, 0.2f, 0.2f),       // Red
        new(1f, 0.5f, 0f),         // Orange
        new(1f, 1f, 0.2f),         // Yellow
        new(0.2f, 0.8f, 0.2f),     // Green
        new(0.2f, 0.4f, 1f),       // Blue
        new(0.6f, 0.2f, 0.8f),     // Purple
        new(0.5f, 0.25f, 0.1f),    // Brown
        new(0.2f, 0.9f, 0.9f),     // Cyan
        new(0.6f, 1f, 0.2f),       // Lime
        new(0.15f, 0.15f, 0.15f),  // Black
        new(0.95f, 0.95f, 0.95f),  // White
        new(1f, 0.6f, 0.8f),       // Pink
        new(0.1f, 0.4f, 0.2f),     // ForteGreen
        new(0.8f, 0.7f, 0.5f)      // Tan
    };

    private VotePlayerInfo _playerInfo;
    private Action<int> _onClickCallback;
    private bool _isReporter = false;

    void Awake()
    {
        if (_voteButton != null)
            _voteButton.onClick.AddListener(OnButtonClicked);
    }

    void OnDestroy()
    {
        if (_voteButton != null)
            _voteButton.onClick.RemoveListener(OnButtonClicked);
    }

    // 슬롯 초기화
    public void Initialize(VotePlayerInfo playerInfo, Action<int> onClickCallback, bool isReporter = false)
    {
        // 이전 슬롯 상태 초기화
        ResetSlot();

        _playerInfo = playerInfo;
        _onClickCallback = onClickCallback;
        _isReporter = isReporter;

        // 플레이어 색상 가져오기
        ApplyPlayerColor();

        UpdateDisplay();
    }

    // 슬롯 상태 초기화
    private void ResetSlot()
    {
        _playerInfo = null;
        _onClickCallback = null;
        _isReporter = false;

        // 텍스트 초기화
        if (_nameText != null)
            _nameText.text = "";

        // 득표 아이콘 모두 제거
        ClearVoteIcons();

        // 모든 마크 비활성화
        if (_deadMark != null) _deadMark.SetActive(false);
        if (_aliveMark != null) _aliveMark.SetActive(false);
        if (_reporterMark != null) _reporterMark.SetActive(false);
        if (_votedMark != null) _votedMark.SetActive(false);

        // 배경색 초기화
        if (_cardBackground != null)
            _cardBackground.color = _aliveBackgroundColor;

        // 아바타 색상 초기화
        if (_colorAvatarImage != null)
            _colorAvatarImage.color = Color.gray;

        // 버튼 활성화
        if (_voteButton != null)
            _voteButton.interactable = true;
    }

    // 득표 아이콘 모두 제거
    private void ClearVoteIcons()
    {
        if (_voteIconContainer == null) return;

        for (int i = _voteIconContainer.childCount - 1; i >= 0; i--)
            Destroy(_voteIconContainer.GetChild(i).gameObject);
    }

    // 플레이어 색상 적용
    private void ApplyPlayerColor()
    {
        if (_colorAvatarImage == null || _playerInfo == null) return;

        // Photon Player CustomProperties에서 색상 가져오기
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Players.TryGetValue(_playerInfo.ActorNumber, out Player player))
        {
            if (player.CustomProperties.TryGetValue(SetPlayerColor.UPPER_COLOR_KEY, out object colorObj))
            {
                PlayerColorType colorType = (PlayerColorType)(int)colorObj;
                int colorIndex = (int)colorType;

                if (colorIndex >= 0 && colorIndex < _playerColors.Length)
                    _colorAvatarImage.color = _playerColors[colorIndex];
                else
                    _colorAvatarImage.color = Color.gray;

                return;
            }
        }

        // 기본 색상
        _colorAvatarImage.color = Color.gray;
    }

    // 표시 업데이트
    public void UpdateDisplay()
    {
        if (_playerInfo == null) return;

        // 이름 표시
        if (_nameText != null)
            _nameText.text = _playerInfo.NickName;

        // 득표 아이콘 업데이트
        UpdateVoteIcons();

        // 생존/사망 상태 표시
        UpdateAliveStatus();

        // 신고자 마크
        if (_reporterMark != null)
            _reporterMark.SetActive(_isReporter);

        // 투표 완료 마크 (이 플레이어가 누군가에게 투표했는지)
        if (_votedMark != null)
            _votedMark.SetActive(_playerInfo.VotedFor != -1);

        // 사망자는 투표 버튼 비활성화
        if (_voteButton != null)
            _voteButton.interactable = !_playerInfo.IsDead;
    }

    // 득표 아이콘 업데이트
    private void UpdateVoteIcons()
    {
        if (_voteIconContainer == null || _voteIconPrefab == null) return;

        // 기존 아이콘 제거
        ClearVoteIcons();

        // 득표 수만큼 아이콘 생성
        for (int i = 0; i < _playerInfo.VoteCount; i++)
            Instantiate(_voteIconPrefab, _voteIconContainer);
    }

    // 생존/사망 상태 업데이트
    private void UpdateAliveStatus()
    {
        bool isDead = _playerInfo.IsDead;

        // 생존/사망 마크
        if (_deadMark != null)
            _deadMark.SetActive(isDead);

        if (_aliveMark != null)
            _aliveMark.SetActive(!isDead);

        // 카드 배경색
        if (_cardBackground != null)
            _cardBackground.color = isDead ? _deadBackgroundColor : _aliveBackgroundColor;
    }

    // 버튼 상호작용 설정
    public void SetInteractable(bool interactable)
    {
        if (_voteButton == null) return;
        if (_playerInfo == null) return;

        // 사망자는 항상 비활성화, 자기 자신 투표 불가
        bool isSelf = _playerInfo.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        _voteButton.interactable = interactable && !_playerInfo.IsDead && !isSelf;
    }

    // 신고자 설정
    public void SetReporter(bool isReporter)
    {
        _isReporter = isReporter;
        if (_reporterMark != null)
            _reporterMark.SetActive(_isReporter);
    }

    private void OnButtonClicked()
    {
        if (_playerInfo == null) return;
        if (_playerInfo.IsDead) return;

        _onClickCallback?.Invoke(_playerInfo.ActorNumber);
    }

    public int ActorNumber => _playerInfo?.ActorNumber ?? -1;
}
