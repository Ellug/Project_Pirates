using UnityEngine;
using TMPro;

// 채팅 입력 담당
// 인풋 필드에서 텍스트 받고 전송
public class ChatInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private ChatLogView _chatLog;

    public void Send()
    {
        string text = _input.text.Trim();

        if (string.IsNullOrEmpty(text)) return;

        _chatLog.AddMessage(text);

        _input.text = "";
        _input.ActivateInputField();
    }
}
