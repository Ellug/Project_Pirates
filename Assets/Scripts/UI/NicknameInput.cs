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

        // Enter Å° Á¦Ãâ
        _nicknameInput.onSubmit.AddListener(OnSubmit);
    }

    private void OnSubmit(string value)
    {
        value = value.Trim();

        if(string.IsNullOrWhiteSpace(value))
        {
            ShowError("´Ð³×ÀÓÀ» ÀÔ·ÂÇØÁÖ¼¼¿ä.");
            return;
        }

        if(value.Contains(" "))
        {
            ShowError("´Ð³×ÀÓ¿¡´Â °ø¹éÀ» »ç¿ëÇÒ ¼ö ¾ø½À´Ï´Ù.");
            return;
        }

        if(!Regex.IsMatch(value, "^[a-zA-Z0-9°¡-ÆR]+$")) // Á¤±ÔÇ¥Çö½Ä (¿µ¹®¼ýÀÚÇÑ±Û)
        {
            ShowError("´Ð³×ÀÓÀº ÇÑ±Û, ¿µ¹®, ¼ýÀÚ¸¸ »ç¿ëÇÒ ¼ö ÀÖ½À´Ï´Ù.");
            return;
        }

        if(value.Length < _minLength)
        {
            ShowError($"´Ð³×ÀÓÀº ÃÖ¼Ò {_minLength}ÀÚ ÀÌ»óÀÌ¾î¾ß ÇÕ´Ï´Ù.");
            return;
        }

        if(value.Length > _maxLength) // Start¿¡¼­ ÀÌ¹Ì Á¦ÇÑµÇ°í ÀÖÁö¸¸, ¾ÈÀüÀ» À§ÇØ ÃÖÁ¾ °ËÁõ
        {
            ShowError($"´Ð³×ÀÓÀº ÃÖ´ë {_maxLength}ÀÚ±îÁö °¡´ÉÇÕ´Ï´Ù.");
            return;
        }

        ConfirmedNickname = value;
        _nicknameInput.text = value;
        ShowWelcome(value);

        OnNicknameConfirmed?.Invoke(ConfirmedNickname);

        Debug.Log($"´Ð³×ÀÓ Á¦Ãâ ¼º°ø : {value}");
    }

    public void TryConfirmCurrentInput() // ¿ÜºÎ¿¡¼­ ¹öÆ° Å¬¸¯ ½Ã »ç¿ëÇÒ ¸Þ¼­µå
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

        _welcomeText.text = $"{nickname}´Ô, È¯¿µÇÕ´Ï´Ù.";
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

        // ±âÁ¸ ÄÚ·çÆ¾ ÁßÁö
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
