using TMPro;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private TextMeshProUGUI _welcomeText;

    public Coroutine _errorCoroutine;

    [Header("Nickname Length Limit")]
    [SerializeField] private int _minLength = 2;
    [SerializeField] private int _maxLength = 8;

    public string ConfirmedNickname { get; private set; } = "";

    public event Action<string> OnNicknameConfirmed;

    private void Start()
    {
        if(_welcomeText != null)
        {
            _welcomeText.gameObject.SetActive(false);
        }

        if(_errorText != null)
        {
            _errorText.gameObject.SetActive(false);
        }

        _nicknameInput.characterLimit = _maxLength;

        // Enter 키 제출
        _nicknameInput.onSubmit.AddListener(OnSubmit);
    }

    private void OnSubmit(string value)
    {
        value = value.Trim();

        if(string.IsNullOrWhiteSpace(value))
        {
            ShowError("닉네임을 입력해주세요.");
            return;
        }

        if(value.Contains(" "))
        {
            ShowError("닉네임에는 공백을 사용할 수 없습니다.");
            return;
        }

        if(!Regex.IsMatch(value, "^[a-zA-Z0-9가-힣]+$")) // 정규표현식 (영문숫자한글)
        {
            ShowError("닉네임은 한글, 영문, 숫자만 사용할 수 있습니다.");
            return;
        }

        if(value.Length < _minLength)
        {
            ShowError($"닉네임은 최소 {_minLength}자 이상이어야 합니다.");
            return;
        }

        if(value.Length > _maxLength) // Start에서 이미 제한하고 있지만, 안전을 위해 최종 검증
        {
            ShowError($"닉네임은 최대 {_maxLength}자까지 가능합니다.");
            return;
        }

        ConfirmedNickname = value;
        _nicknameInput.text = value;
        ShowWelcome(value);

        OnNicknameConfirmed?.Invoke(ConfirmedNickname);

        Debug.Log($"닉네임 제출 성공 : {value}");
    }

    public void TryConfirmCurrentInput() // 외부에서 버튼 클릭 시 사용할 메서드
    {
        if(_nicknameInput == null)
        {
            return;
        }

        OnSubmit(_nicknameInput.text);
    }

    private void ShowWelcome(string nickname)
    {
        if(_welcomeText == null)
        {
            return;
        }

        _welcomeText.text = $"{nickname}님, 환영합니다.";
        _welcomeText.gameObject.SetActive(true);
    }

    private void ShowError(string txt)
    {
        if(_errorText == null)
        {
            return;
        }

        _errorText.text = txt;
        _errorText.gameObject.SetActive(true);

        // 기존 코루틴 중지
        if(_errorCoroutine != null)
        {
            StopCoroutine(_errorCoroutine);
        }

        _errorCoroutine = StartCoroutine(HideAfterSeconds(1.8f));
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        _errorText.gameObject.SetActive(false);
        _errorCoroutine = null;
    }
}
