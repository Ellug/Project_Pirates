using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUnitView : MonoBehaviour
{
    [SerializeField] private TMP_Text _roomName;
    [SerializeField] private TMP_Text _playerCount;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Button _joinButton;

    private RoomSnapshot _snapshot;
    private Action<RoomSnapshot> _onJoinRequested;

    void Awake()
    {
        if (_joinButton != null)
            _joinButton.onClick.AddListener(HandleJoinClicked);
    }

    void OnDestroy()
    {
        if (_joinButton != null)
            _joinButton.onClick.RemoveListener(HandleJoinClicked);
    }

    public void Bind(in RoomSnapshot snapshot, Action<RoomSnapshot> onJoinRequested)
    {
        _snapshot = snapshot;
        _onJoinRequested = onJoinRequested;

        _roomName.text = snapshot.Title;
        _playerCount.text = $"{snapshot.PlayerCount} / {snapshot.MaxPlayers}";
        _lockIcon.gameObject.SetActive(snapshot.HasPassword);

        // 방이 닫혔거나 꽉 찼으면 버튼 비활성화
        bool canJoin = snapshot.IsOpen && snapshot.PlayerCount < snapshot.MaxPlayers;
        if (_joinButton != null) _joinButton.interactable = canJoin;
    }

    private void HandleJoinClicked()
    {
        if (!_snapshot.IsValid)
        {
            Debug.LogWarning("[RoomUnitView] Join ignored: invalid snapshot.");
            return;
        }

        _onJoinRequested?.Invoke(_snapshot);
    }
}
