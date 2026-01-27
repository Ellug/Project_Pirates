using System;
using TMPro;
using UnityEngine;

public class CreateRoomPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_InputField _title;
    [SerializeField] private TMP_InputField _pw;
    [SerializeField] private TMP_Dropdown _maxPlayers;

    public event Action<CreateRoomRequest> ApplyRequested;
    public event Action CancelRequested;

    public void Open()
    {
        if (_panelRoot == null) return;
        _panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (_panelRoot == null) return;
        _panelRoot.SetActive(false);
    }

    // 인스펙터 버튼 연결
    public void OnClickApply()
    {
        string name = _title != null ? _title.text : string.Empty;
        string pw = _pw != null ? _pw.text : string.Empty;
        int maxPlayers = _maxPlayers != null ? (_maxPlayers.value + 5) : 2;

        ApplyRequested?.Invoke(new CreateRoomRequest(name, pw, maxPlayers));
        Close();
    }

    public void OnClickCancel()
    {
        CancelRequested?.Invoke();
        Close();
    }
}
