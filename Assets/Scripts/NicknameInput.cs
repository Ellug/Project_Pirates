using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameInput : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nicknameInput;
    public TextMeshProUGUI errorText;

    private void Start()
    {
        if(errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }

        nicknameInput.onSubmit.AddListener(OnSubmit);
    }

    void OnSubmit(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            ShowError("Please enter a nickname.");
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
    }
}
