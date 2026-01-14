using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 채팅 로그 출력 담당
// 메세지를 생성해서 Content 에 붙이기, 스크롤 아래로 이동
public class ChatLogView : MonoBehaviour
{
    [SerializeField] private Transform _content;
    [SerializeField] ScrollRect _scrollRect;
    [SerializeField] GameObject _chatPrefab;

    public void AddMessage(string text)
    {
        GameObject msg = Instantiate(_chatPrefab, _content);
        msg.GetComponent<TextMeshProUGUI>().text = text;
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
