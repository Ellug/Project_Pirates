using TMPro;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private TextMeshProUGUI _welcomeText;

    private Coroutine _errorCoroutine;

    [Header("Nickname Length Limit")]
    [SerializeField] private int _minLength = 2;
    [SerializeField] private int _maxLength = 8;

    public string ConfirmedNickname { get; private set; } = "";

    private void Start()
    {
        if (_welcomeText != null)
            _welcomeText.gameObject.SetActive(false);

        if (_errorText != null)
            _errorText.gameObject.SetActive(false);

        if (_nicknameInput != null)
            _nicknameInput.characterLimit = _maxLength;
    }

    // 한 프레임 쉬고 포커스 해제서 IME 조합 완료 유도
    public IEnumerator CorConfirmIme()
    {
        if (_nicknameInput == null)
            yield break;

        // 한 프레임 넘겨
        yield return null;

        // 포커스 해제
        if (_nicknameInput.isFocused)
            _nicknameInput.DeactivateInputField(false);

        _nicknameInput.ForceLabelUpdate();
    }

    public bool TryConfirmCurrentInput()
    {
        if (_nicknameInput == null)
            return false;

        return TryValidate(_nicknameInput.text);
    }

    private bool TryValidate(string value)
    {
        value = value.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return Fail("닉네임을 입력해주세요.");

        if (value.Contains(" "))
            return Fail("닉네임에는 공백을 사용할 수 없습니다.");

        if (!Regex.IsMatch(value, "^[a-zA-Z0-9가-힣]+$"))
            return Fail("닉네임은 한글, 영문, 숫자만 사용할 수 있습니다.");

        if (value.Length < _minLength)
            return Fail($"닉네임은 최소 {_minLength}자 이상이어야 합니다.");

        if (value.Length > _maxLength)
            return Fail($"닉네임은 최대 {_maxLength}자까지 가능합니다.");

        ConfirmedNickname = value;
        _nicknameInput.text = value;
        ShowWelcome(value);

        Debug.Log($"닉네임 검증 성공 : {value}");
        return true;
    }

    private bool Fail(string message)
    {
        ShowError(message);
        return false;
    }

    private void ShowWelcome(string nickname)
    {
        if (_welcomeText == null)
            return;

        _welcomeText.text = $"{nickname}님, 환영합니다.";
        _welcomeText.gameObject.SetActive(true);
    }

    private void ShowError(string txt)
    {
        if (_errorText == null)
            return;

        _errorText.text = txt;
        _errorText.gameObject.SetActive(true);

        if (_errorCoroutine != null)
            StopCoroutine(_errorCoroutine);

        _errorCoroutine = StartCoroutine(HideAfterSeconds(1.8f));
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _errorText.gameObject.SetActive(false);
        _errorCoroutine = null;
    }
}
