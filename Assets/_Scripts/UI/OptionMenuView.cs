using Photon.Pun;
using Photon.Voice.PUN;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionMenuView : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _graphicPanel;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _voicePanel;
    [SerializeField] private GameObject _controllPanel;

    private bool _isOnclickedOut = false;

    public void CloseOptionPanel()
    {
        _root.SetActive(false);
    }

    public void OnClickTab(GameObject Panel)
    {
        CloseAllTabs();
        Panel.SetActive(true);
    }

    private void CloseAllTabs()
    {
        _graphicPanel.SetActive(false);
        _audioPanel.SetActive(false);
        _voicePanel.SetActive(false);
        _controllPanel.SetActive(false);
    }

    public void OnClickOut()
    {
        Debug.Log($"현재 상태: {GameManager.Instance.FlowState}, 연결상태: {PhotonNetwork.IsConnected}");

        if (_isOnclickedOut) return;
        _isOnclickedOut = true;
        Debug.Log("나가는중...");
        switch (GameManager.Instance.FlowState)
        {
            case SceneState.Title:
                Application.Quit();
                CloseOptionPanel();
                break;

            case SceneState.Lobby:
                PhotonNetwork.Disconnect();
                GoToTitle();
                break;

            case SceneState.Room:
            case SceneState.InGame:
                StartCoroutine(LeaveRoom());
                break;
        }
    }

    //타이틀로 나가기
    private void GoToTitle()
    {
        Debug.Log("서버 연결 종료 완료 -> 타이틀로");
        _isOnclickedOut = false;
        SceneManager.LoadScene("Title");
    }

    private IEnumerator LeaveRoom()
    {
        var voiceClient = PunVoiceClient.Instance;
        if (voiceClient != null && voiceClient.Client != null && voiceClient.Client.IsConnected)
        {
            voiceClient.Disconnect();
            Debug.Log("VoiceClient Disconnect 요청...");

            yield return new WaitUntil(() => !voiceClient.Client.IsConnected);
        }

        PhotonNetwork.LeaveRoom();
        Debug.Log("LeaveRoom 요청...");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OptionMenuView의 OnLeftRoom 콜백 실행됨");
        _isOnclickedOut = false;
        Debug.Log("현재 씬" + GameManager.Instance.FlowState);
        if (GameManager.Instance.FlowState == SceneState.Room) return;
        Debug.Log($"룸씬이 아니니깐 여기 들어옴. ({GameManager.Instance.FlowState} 씬)");
        
        if (PlayerManager.Instance != null)
            Destroy(PlayerManager.Instance.gameObject);
        Debug.Log("PlayerManager 파괴됨");

        SceneManager.LoadScene("Lobby");
    }
}
