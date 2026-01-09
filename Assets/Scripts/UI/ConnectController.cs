using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ConnectController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NicknameInput _nicknameInput;
    [SerializeField] private TitleManager _titleManager;

    private bool _isHandling;

    void Start()
    {
        InputSystem.actions["Submit"].started += OnClickEnter;
    }

    private void OnDestroy()
    {
        InputSystem.actions["Submit"].started -= OnClickEnter;
    }

    // 연결 온클릭 이벤트 연결 to 버튼
    public void OnClickConnect()
    {
        HandleSubmit();
    }
    private void OnClickEnter(InputAction.CallbackContext ctx)
    {
        HandleSubmit();
    }


    // 제출 메서드
    private void HandleSubmit()
    {
        if (_isHandling) return; // 이미 처리 중이면 무시

        if (_nicknameInput == null || _titleManager == null) return;

        _isHandling = true;
        StartCoroutine(CorHandleSubmit());
    }

    private IEnumerator CorHandleSubmit()
    {
        yield return null; // 한 프레임 스킵

        // 확정 닉네임 없으면 Enter/Btn 모두 확정 시도
        if (string.IsNullOrEmpty(_nicknameInput.ConfirmedNickname))
        {
            // IME 조합 완료 대기
            yield return _nicknameInput.CorConfirmIme();

            // 현재 입력값으로 검증 시도
            bool ok = _nicknameInput.TryConfirmCurrentInput();

            _isHandling = false;

            if (!ok) yield break; // 확정 실패면 여기서 종료
        }

        // 확정 닉네임이 있으면 연결 시도
        _titleManager.ConnectToServer(_nicknameInput.ConfirmedNickname);
        _isHandling = false;
    }
}