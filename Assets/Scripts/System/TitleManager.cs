using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private string _nickName = "DevPlayer";

    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Title);
    }

    // 임시 C키로 연결
    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Debug.Log("C Key Pressed");
            ConnectToServer();
        }
    }

    // 서버 연결 메서드. 버튼에 연결하던 프라이빗으로 바꿔서 메서드에 연결하던 해서 사용.
    public void ConnectToServer()
    {
        Debug.Log("Connect To Server" + _nickName);
        PhotonNetwork.NickName = "DevPlayer";
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 연결 콜백
    public override void OnConnectedToMaster()
    {
        Debug.Log("CB : Complete Connect to Master");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("CB : On Joined Lobby -> Go to Lobby");
        SceneManager.LoadScene("Lobby");
    }
}
