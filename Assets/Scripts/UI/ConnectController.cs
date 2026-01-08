using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ConnectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NicknameInput _nicknameInput;
    [SerializeField] private TitleManager _titleManager;

    private bool _isTryingConnect;

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
        if(Keyboard.current.enterKey.wasPressedThisFrame)
        TryConnect();
    }

    // 연결 시도
    private void TryConnect()
    {
        if (_nicknameInput == null || _titleManager == null) return;

        _isTryingConnect = true;
        StartCoroutine(CorTryConnect());

        //if (_nicknameInput.TryConfirmCurrentInput())
        //    _titleManager.ConnectToServer(_nicknameInput.ConfirmedNickname);
    }

    private IEnumerator CorTryConnect()
    {
        // 한글 IME 완료까지 대기
        yield return _nicknameInput.CorConfirmIme();

        // 최종 text로 검증
        bool ok = _nicknameInput.TryConfirmCurrentInput();
        _isTryingConnect = false;

        if (!ok)
            yield break;

        // 검증 성공하면 연결
        _titleManager.ConnectToServer(_nicknameInput.ConfirmedNickname);
    }
}