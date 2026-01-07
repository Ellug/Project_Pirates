using System.Collections;
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

        // Enter 키 제출
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

        if(value.Length < minLength)
        {
            ShowError($"Nickname must be at least {minLength} characters");
            return;
        }

        if(value.Length > maxLength)
        {
            ShowError($"Nickname must be {maxLength} characters or less.");
            return;
        }

        Debug.Log($"닉네임 제출 성공 : {value}");
    }

    void ShowError(string txt)
    {
        if(errorText == null)
        {
            return;
        }

        errorText.text = txt;
        errorText.gameObject.SetActive(true);

        // 기존 코루틴 중지
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
