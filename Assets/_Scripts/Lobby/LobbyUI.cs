using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("NickName")]
    [SerializeField] private TMP_Text _nickNameText;

    [Header("UI")]
    [SerializeField] private RoomListView _roomListView;
    [SerializeField] private CreateRoomPanelView _createRoomPanel;
    [SerializeField] private JoinPwPanelView _joinPwPanel;

    public event Action RefreshRequested;
    public event Action<CreateRoomRequest> CreateRoomRequested;
    public event Action<RoomSnapshot> JoinRequested;
    public event Action<RoomSnapshot, string> PasswordJoinRequested;
    public event Action QuickStartRequested;
    public event Action LeaveToTitleRequested;

    void Awake()
    {
        if (_roomListView != null)
            _roomListView.JoinClicked += HandleRoomJoinClicked;

        if (_createRoomPanel != null)
            _createRoomPanel.ApplyRequested += HandleCreateRoomApplyRequested;

        if (_joinPwPanel != null)
            _joinPwPanel.ApplyRequested += HandleJoinPwApplyRequested;
    }

    void OnDestroy()
    {
        if (_roomListView != null)
            _roomListView.JoinClicked -= HandleRoomJoinClicked;

        if (_createRoomPanel != null)
            _createRoomPanel.ApplyRequested -= HandleCreateRoomApplyRequested;

        if (_joinPwPanel != null)
            _joinPwPanel.ApplyRequested -= HandleJoinPwApplyRequested;
    }

    public void SetNickname(string nickname)
    {
        if (_nickNameText != null)
            _nickNameText.text = nickname;
    }

    public void RenderRooms(IReadOnlyList<RoomSnapshot> rooms)
    {
        if (_roomListView != null)
            _roomListView.Render(rooms);
    }

    public void OpenJoinPassword(RoomSnapshot snap)
    {
        _joinPwPanel?.Open(snap);
    }

    public void CloseJoinPassword()
    {
        _joinPwPanel?.Close();
    }

    public void ShowJoinPasswordError(string message)
    {
        _joinPwPanel?.ShowError(message);
    }

    // ===== Buttons (인스펙터 연결) =====

    public void OnClickRefresh()
    {
        RefreshRequested?.Invoke();
    }

    public void OnClickOpenCreateRoom()
    {
        _createRoomPanel?.Open();
    }

    public void OnClickQuickStart()
    {
        QuickStartRequested?.Invoke();
    }

    public void OnClickLeaveToTitle()
    {
        LeaveToTitleRequested?.Invoke();
    }

    // ===== Internal =====

    // 룸 조인 클릭시
    private void HandleRoomJoinClicked(RoomSnapshot snap)
    {
        if (!snap.IsValid) return;

        // 비밀 방이면 패스워드 패널 열기
        if (snap.HasPassword)
        {
            OpenJoinPassword(snap);
            return;
        }

        JoinRequested?.Invoke(snap);
    }

    private void HandleCreateRoomApplyRequested(CreateRoomRequest req)
    {
        CreateRoomRequested?.Invoke(req);
    }

    private void HandleJoinPwApplyRequested(RoomSnapshot snap, string pw)
    {
        PasswordJoinRequested?.Invoke(snap, pw);
    }
}
