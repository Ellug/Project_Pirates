using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoomPlayerContentView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _profileImage;
    [SerializeField] private TMP_Text _nickName;
    [SerializeField] private GameObject _markMaster;
    [SerializeField] private Button _kickButton;

    [Header("Ready Indicator (color)")]
    [SerializeField] private Image _readyIndicator;

    private Color _defaultNickColor;
    private Player _boundPlayer;
    private Action<Player> _onKick;

    void Awake()
    {
        if (_nickName != null)
            _defaultNickColor = _nickName.color;

        if (_kickButton != null)
            _kickButton.onClick.AddListener(HandleKickClicked);
    }

    void OnDestroy()
    {
        if (_kickButton != null)
            _kickButton.onClick.RemoveListener(HandleKickClicked);
    }

    public void Bind(Player player, bool isMe, bool isMaster, bool isReady, Color meNickColor, Color readyOnColor, Color readyOffColor, bool kickEnabled, Action<Player> onKick)
    {
        _boundPlayer = player;
        _onKick = onKick;

        // Nickname
        if (_nickName != null)
        {
            string name = (player == null || string.IsNullOrWhiteSpace(player.NickName))
                ? $"Player {(player != null ? player.ActorNumber : 0)}"
                : player.NickName;

            _nickName.text = isMe ? $"{name} (ME)" : name;
            _nickName.color = isMe ? meNickColor : _defaultNickColor;
        }

        // Master mark
        if (_markMaster != null)
            _markMaster.SetActive(isMaster);

        // Ready indicator color
        if (_readyIndicator != null)
            _readyIndicator.color = isReady ? readyOnColor : readyOffColor;

        // Kick button
        if (_kickButton != null)
        {
            _kickButton.gameObject.SetActive(kickEnabled);
            _kickButton.interactable = kickEnabled;
        }
    }

    public void Unbind()
    {
        _boundPlayer = null;
        _onKick = null;

        if (_markMaster != null) _markMaster.SetActive(false);
        if (_kickButton != null) _kickButton.gameObject.SetActive(false);
    }

    private void HandleKickClicked()
    {
        if (_boundPlayer == null) return;
        _onKick?.Invoke(_boundPlayer);
    }
}
