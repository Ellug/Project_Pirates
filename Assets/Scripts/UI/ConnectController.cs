using UnityEngine;
using UnityEngine.InputSystem;

public class ConnectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NicknameInput _nicknameInput;
    [SerializeField] private TitleManager _titleManager;

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
            TryConnect();
    }

    // 버튼 OnClick => Submit
    // NicknameInput은 검사 및 닉네임 확정 역할만
    // TitleManager가 접속 요청 및 콜백 처리함
    public void OnClickConnect()
    {
        TryConnect();
    }

    // 연결 시도
    private void TryConnect()
    {
        if (_nicknameInput == null || _titleManager == null) return;

        
        if (_nicknameInput.TryConfirmCurrentInput())
            _titleManager.ConnectToServer(_nicknameInput.ConfirmedNickname);
    }
}