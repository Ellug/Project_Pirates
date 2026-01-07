using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nicknameInput;
    public TextMeshProUGUI errorText;

    public Coroutine errorCoroutine;

    [Header("Nickname Length Limit")]
    [SerializeField] private int minLength = 2;
    [SerializeField] private int maxLength = 8;

    private void Start()
    {
        if(errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }

        nicknameInput.characterLimit = maxLength;

        // Enter ≈∞ ¡¶√‚
        nicknameInput.onSubmit.AddListener(OnSubmit);
    }

    void OnSubmit(string value)
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

        if(!Regex.IsMatch(value, "^[a-zA-Z0-9∞°-∆R]+$"))
        {
            ShowError("Nickname can only contain letters and numbers.");
            return;
        }

        if(value.Length < minLength)
        {
            ShowError($"Nickname must be at least {minLength} characters");
            return;
        }

        if(value.Length > maxLength) // Startø°º≠ ¿ÃπÃ ¡¶«—µ«∞Ì ¿÷¡ˆ∏∏, æ»¿¸¿ª ¿ß«ÿ √÷¡æ ∞À¡ı
        {
            ShowError($"Nickname must be {maxLength} characters or less.");
            return;
        }

        Debug.Log($"¥–≥◊¿” ¡¶√‚ º∫∞¯ : {value}");
    }

    void ShowError(string txt)
    {
        if(errorText == null)
        {
            return;
        }

        errorText.text = txt;
        errorText.gameObject.SetActive(true);

        // ±‚¡∏ ƒ⁄∑Á∆æ ¡ﬂ¡ˆ
        if(errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }

        errorCoroutine = StartCoroutine(HideAfterSeconds(1.5f));
    }

    IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        errorText.gameObject.SetActive(false);
        errorCoroutine = null;
    }
}
