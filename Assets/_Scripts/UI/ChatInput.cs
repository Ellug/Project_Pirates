using UnityEngine;
using TMPro;

// 채팅 입력 담당
// 인풋 필드에서 텍스트 받고 전송
public class ChatInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private ChatNetwork _network;

    public void Send()
    {
        string text = _input.text.Trim();

        if (string.IsNullOrEmpty(text)) return;

        _network.SendChat(text);

        _input.text = "";
        _input.ActivateInputField();
    }
}
