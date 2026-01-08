using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Title);
    }

    // 서버 연결
    public void ConnectToServer(string nickname)
    {
        Debug.Log($"Connect To Server : {nickname}");
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 연결 콜백
    public override void OnConnectedToMaster()
    {
        Debug.Log("CB : Complete Connect to Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("CB : On Joined Lobby -> Go to Lobby");
        SceneManager.LoadScene("Lobby");
    }
}
