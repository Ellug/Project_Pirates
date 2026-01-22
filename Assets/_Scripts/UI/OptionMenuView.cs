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

            case SceneState.Room :
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
        Debug.Log("LeaveRoom 코루틴 진입...");
        var voiceClient = PunVoiceClient.Instance;
        if (voiceClient != null && voiceClient.Client != null && voiceClient.Client.IsConnected)
        {
            voiceClient.Disconnect();
            Debug.Log("VoiceClient Disconnect 요청...");
            yield return new WaitUntil(() => !voiceClient.Client.IsConnected);
            Debug.Log("[Room] Voice client disconnected.");
        }

        _isOnclickedOut = false;
        PhotonNetwork.LeaveRoom();
        Debug.Log("LeaveRoom 요청...");

        //RoomManager의 콜백이 없는 상황(InGame)
        if (GameManager.Instance.FlowState != SceneState.Room)
        {
            yield return new WaitForSeconds(0.2f);
            Debug.Log("[InGame] InGameScene -> Go to Lobby");
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.GameOver();
            SceneManager.LoadScene("Lobby");
        }
    }
}
