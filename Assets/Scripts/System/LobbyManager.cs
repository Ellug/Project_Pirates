using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private LobbyUI _lobbyUI;

    private Dictionary<string, RoomInfo> _cachedRoomList = new();

    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Lobby);
        StartCoroutine(ConnectCheckCoroutine());
    }

    private IEnumerator ConnectCheckCoroutine() 
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
        Debug.Log("Master 서버 연결 완료");

        PhotonNetwork.JoinLobby();

        yield return new WaitUntil(() => PhotonNetwork.InLobby);
        Debug.Log("로비 입장 완료");

        RefreshRoomUI();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        LobbyUI.OnCreateRoomRequest += CreateRoom;
        LobbyUI.OnRefreshRoomListRequest += RefreshRoomUI;
        RoomPrefab.OnTryJoinRoom += TryJoinRoom;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        LobbyUI.OnCreateRoomRequest -= CreateRoom;
        LobbyUI.OnRefreshRoomListRequest -= RefreshRoomUI;
        RoomPrefab.OnTryJoinRoom -= TryJoinRoom;
    }

    void Update()
    {
        
    }

    // Method
    public void OnClickQuickStart()
    {
        ExitGames.Client.Photon.Hashtable expectedProps =
        new ExitGames.Client.Photon.Hashtable
        {
            { "pw", string.Empty }
        };

        PhotonNetwork.JoinRandomRoom(expectedProps, 0);
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
        if (roomInfo == null)
        {
            Debug.LogWarning("TryJoinRoom 호출 시 RoomInfo가 null!");
            return;
        }
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

    //RoomListUI
    private void RefreshRoomUI()
    {
        foreach (Transform child in _lobbyUI.RoomListPanel)
        {
            Destroy(child.gameObject);
        }

        if (_cachedRoomList.Count == 0)
        {
            _lobbyUI.EmptyText.text = "방이 없습니다...";
            return;
        }

        _lobbyUI.EmptyText.text = string.Empty;

        foreach (var roomInfo in _cachedRoomList.Values)
        {
            var room = Instantiate(_lobbyUI.RoomPrefab, _lobbyUI.RoomListPanel);

            var roomList = room.GetComponent<RoomPrefab>();

            roomList.Init(roomInfo);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var info in roomList)
        {
            if (info.RemovedFromList)
            {
                _cachedRoomList.Remove(info.Name);
            }
            else
            {
                _cachedRoomList[info.Name] = info;
            }
        }
        RefreshRoomUI();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LeaveLobby();
    }
}
