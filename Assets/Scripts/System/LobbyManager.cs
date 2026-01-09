using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Lobby);
        PhotonNetwork.JoinLobby();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        LobbyUI.OnCreateRoomRequest += CreateRoom;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        LobbyUI.OnCreateRoomRequest -= CreateRoom;
    }

    void Update()
    {
        
    }

    // Method
    public void OnClickQuickStart()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void CreateRoom()
    {
        Debug.Log("[Lobby] Create Room");
        PhotonNetwork.CreateRoom("room_00", new RoomOptions { MaxPlayers = 8});
    }

    public void CreateRoom(string roomName, string roomPW, int maxPlayer)
    {
        bool hasPassword = !string.IsNullOrEmpty(roomPW);

        ExitGames.Client.Photon.Hashtable customProps =
            new ExitGames.Client.Photon.Hashtable
            {
                { "pw", roomPW }
            };
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayer,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = customProps,
            CustomRoomPropertiesForLobby = new string[] { "pw" }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void TryJoinRoom(RoomInfo roomInfo, string inputPW)
    {
        if (roomInfo.CustomProperties.TryGetValue("pw", out object pwObj))
        {
            string roomPW = pwObj as string;
            if (roomPW != inputPW)
            {
                Debug.Log("비밀번호 틀림");
                return;
            }
        }
        PhotonNetwork.JoinRoom(roomInfo.Name);
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
