using System;
using TMPro;
using UnityEngine;

public class JoinPwPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private TMP_Text _roomNameText;
    [SerializeField] private TMP_Text _errorText;

    public event Action<RoomSnapshot, string> ApplyRequested;
    public event Action CancelRequested;

    private RoomSnapshot _pending;
    private bool _hasPending;

    void Awake()
    {
        SetError(string.Empty);

        if (_input != null)
            _input.onSubmit.AddListener(_ => Submit());
    }

    void OnDestroy()
    {
        if (_input != null)
            _input.onSubmit.RemoveAllListeners();
    }

    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    // 비밀번호 패널 열기
    public void Open(RoomSnapshot snap)
    {
        if (_panelRoot == null || !snap.IsValid)
            return;

        _pending = snap;
        _hasPending = true;

        if (_roomNameText != null)
            _roomNameText.text = snap.Name;

        if (_input != null)
        {
            _input.text = string.Empty;
            _input.ActivateInputField();
        }

        SetError(string.Empty);
        _panelRoot.SetActive(true);
    }

    // 비밀번호 패널 닫기
    public void Close()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        _hasPending = false;
        _pending = default;

        if (_input != null)
            _input.text = string.Empty;

        SetError(string.Empty);
    }

    public void ShowError(string message)
    {
        SetError(message);
    }

    // === 확인, 취소 // 인스펙터 버튼 연결 ===
    public void OnClickApply()
    {
        Submit();
    }

    public void OnClickCancel()
    {
        CancelRequested?.Invoke();
        Close();
    }

    // 제출
    private void Submit()
    {
        if (!_hasPending)
        {
            SetError("방 정보를 찾을 수 없습니다.");
            return;
        }

        string pw = _input != null ? _input.text : string.Empty;
        ApplyRequested?.Invoke(_pending, pw);
    }
    
    // 에러 메세지 세팅
    private void SetError(string message)
    {
        if (_errorText == null)
            return;

        bool has = !string.IsNullOrWhiteSpace(message);
        _errorText.gameObject.SetActive(has);
        if (has) _errorText.text = message;
    }
}
