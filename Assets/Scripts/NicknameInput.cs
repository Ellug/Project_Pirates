using TMPro;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField _nicknameInput;
    public TextMeshProUGUI _errorText;

    public Coroutine _errorCoroutine;

    [Header("Nickname Length Limit")]
    [SerializeField] private int _minLength = 2;
    [SerializeField] private int _maxLength = 8;

    public string ConfirmedNickname { get; private set; } = "";

    public event Action<string> OnNicknameConfirmed;

    private void Start()
    {
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
            ShowError("Please enter a nickname.");
            return;
        }

        if(value.Contains(" "))
        {
            ShowError("Nickname cannot contain spaces");
            return;
        }

        if(!Regex.IsMatch(value, "^[a-zA-Z0-9°¡-ÆR]+$")) // Á¤±ÔÇ¥Çö½Ä (¿µ¹®¼ýÀÚÇÑ±Û)
        {
            ShowError("Nickname can only contain letters and numbers.");
            return;
        }

        if(value.Length < _minLength)
        {
            ShowError($"Nickname must be at least {_minLength} characters");
            return;
        }

        if(value.Length > _maxLength) // Start¿¡¼­ ÀÌ¹Ì Á¦ÇÑµÇ°í ÀÖÁö¸¸, ¾ÈÀüÀ» À§ÇØ ÃÖÁ¾ °ËÁõ
        {
            ShowError($"Nickname must be {_maxLength} characters or less.");
            return;
        }

        ConfirmedNickname = value;
        _nicknameInput.text = value;
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

        _errorCoroutine = StartCoroutine(HideAfterSeconds(1.5f));
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        _errorText.gameObject.SetActive(false);
        _errorCoroutine = null;
    }
}
