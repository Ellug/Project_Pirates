using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Lobby);
    }

    // 임시 인풋
    void Update()
    {
        // J : 랜덤 방 입장 (없으면 생성)
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            Debug.Log("[Lobby] J pressed → JoinRandomOrCreateRoom");

            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("[Lobby] Cannot join room (Photon not ready)");                
                return;
            }

            QuickStart();
        }

        // K : 기본 설정으로 방 생성
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("[Lobby] K pressed → CreateRoom (default)");

            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("[Lobby] Cannot create room (Photon not ready)");
                return;
            }

            CreateRoom();            
        }
    }

    // Method
    public void QuickStart()
    {
        Debug.Log("[Lobby] Quick Start!");
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public void CreateRoom()
    {
        Debug.Log("[Lobby] Create Room");
        PhotonNetwork.CreateRoom("room_00", new RoomOptions { MaxPlayers = 8});
    }

    public void JoinRoom()
    {
        // 그냥 조인이 아니라 방 선택해서 해야함
    }

    public void LeaveLobby()
    {
        PhotonNetwork.LeaveLobby();
    }

    // CB
    public override void OnJoinedRoom()
    {
        Debug.Log("Lobby CB : 룸 입장");
        SceneManager.LoadScene("Room");
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Lobby CB : 로비에서 나갔음");
        SceneManager.LoadScene("Title");
    }
}
