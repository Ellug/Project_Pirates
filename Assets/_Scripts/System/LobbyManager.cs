using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private const string ROOM_PW_KEY = "pw";
    private const string ROOM_TITLE_KEY = "title";

    [SerializeField] private LobbyUI _ui;

    [Header("Refresh")]
    [SerializeField] private float _autoRefreshIntervalSeconds = 10f;

    private bool _leaveToTitleRequested;
    private Coroutine _autoRefreshCo;

    private readonly Dictionary<string, RoomInfo> _cachedRoomList = new();

    void Awake()
    {
        if (_ui == null) return;

        _ui.RefreshRequested += ManualRefresh_RenderFromCache;

        _ui.CreateRoomRequested += HandleCreateRoomRequested;
        _ui.JoinRequested += HandleJoinRequested;
        _ui.PasswordJoinRequested += HandlePasswordJoinRequested;

        _ui.QuickStartRequested += HandleQuickStartRequested;
        _ui.LeaveToTitleRequested += HandleLeaveToTitleRequested;
    }

    void OnDestroy()
    {
        StopAutoRefresh();

        if (_ui == null) return;

        _ui.RefreshRequested -= ManualRefresh_RenderFromCache;

        _ui.CreateRoomRequested -= HandleCreateRoomRequested;
        _ui.JoinRequested -= HandleJoinRequested;
        _ui.PasswordJoinRequested -= HandlePasswordJoinRequested;

        _ui.QuickStartRequested -= HandleQuickStartRequested;
        _ui.LeaveToTitleRequested -= HandleLeaveToTitleRequested;
    }

    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Lobby);
        StartCoroutine(ConnectCheckCoroutine());
    }

    private IEnumerator ConnectCheckCoroutine()
    {
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
        Debug.Log("[Lobby] Connected to Master");

        PhotonNetwork.JoinLobby();
        yield return new WaitUntil(() => PhotonNetwork.InLobby);
        Debug.Log("[Lobby] Joined Lobby");

        _ui?.SetNickname(PhotonNetwork.NickName);

        // 최초 1회 렌더 (캐시가 비어도 EmptyText 처리)
        RenderRoomsFromCache("Initial Render");

        // 자동 리프레시 시작
        StartAutoRefresh();
    }

    // ===== Photon Callbacks =====

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            var info = roomList[i];

            if (info.RemovedFromList)
                _cachedRoomList.Remove(info.Name);
            else
                _cachedRoomList[info.Name] = info;
        }

        RenderRoomsFromCache($"OnRoomListUpdate (delta={roomList.Count})");
    }

    // ===== Refresh =====

    // 버튼용: 캐시 기반 리프레쉬
    private void ManualRefresh_RenderFromCache()
    {
        RenderRoomsFromCache("Manual Refresh Button");
    }

    // 주기용: 10초마다 캐시 기반 리프레쉬
    private void StartAutoRefresh()
    {
        if (_autoRefreshCo != null) return;

        Debug.Log($"[Lobby] AutoRefresh started: every {_autoRefreshIntervalSeconds:0}s (render from cache)");
        _autoRefreshCo = StartCoroutine(Co_AutoRefreshRender());
    }

    private void StopAutoRefresh()
    {
        if (_autoRefreshCo == null) return;

        StopCoroutine(_autoRefreshCo);
        _autoRefreshCo = null;
        Debug.Log("[Lobby] AutoRefresh stopped");
    }

    private IEnumerator Co_AutoRefreshRender()
    {
        var wait = new WaitForSeconds(_autoRefreshIntervalSeconds);

        while (true)
        {
            yield return wait;

            if (_leaveToTitleRequested)
                continue;

            RenderRoomsFromCache("Auto Refresh Tick");
        }
    }

    // 캐시 -> Snapshot -> UI 렌더
    private void RenderRoomsFromCache(string reason)
    {
        if (_ui == null)
        {
            Debug.LogWarning($"[Lobby] Render skipped (ui null). reason={reason}");
            return;
        }

        var snaps = new List<RoomSnapshot>(_cachedRoomList.Count);

        foreach (var info in _cachedRoomList.Values)
        {
            string title = info.Name;

            if (info.CustomProperties != null && info.CustomProperties.TryGetValue(ROOM_TITLE_KEY, out object titleObj))
                title = titleObj as string ?? title;

            bool hasPw =
                info.CustomProperties != null &&
                info.CustomProperties.TryGetValue("pw", out object pwObj) &&
                !string.IsNullOrEmpty(pwObj as string);

            snaps.Add(new RoomSnapshot(
                info.Name,
                title,
                info.PlayerCount,
                (int)info.MaxPlayers,
                hasPw,
                info.IsOpen
            ));
        }

        _ui.RenderRooms(snaps);
        Debug.Log($"[Lobby] RenderRoomsFromCache: {snaps.Count} rooms. reason={reason}");
    }

    // ===== Create Room =====

    private void HandleCreateRoomRequested(CreateRoomRequest req)
    {
        Debug.Log("[Lobby] HandleCreateRoomRequested");

        // 내부 식별자(방 이름). 중복 방지 위해 Guid/랜덤 추천
        string roomName = string.IsNullOrWhiteSpace(req.Name)
            ? $"room_{System.Guid.NewGuid():N}".Substring(0, 12)
            : req.Name;

        // 표시용 제목(= title 커스텀 프로퍼티)
        string title = string.IsNullOrWhiteSpace(req.Name) ? "No Title" : req.Name;

        string roomPW = req.Password ?? string.Empty;
        int maxPlayer = Mathf.Clamp(req.MaxPlayers, 1, 16);

        var props = new ExitGames.Client.Photon.Hashtable
        {
            { ROOM_TITLE_KEY, title },
            { ROOM_PW_KEY, roomPW }
        };

        var options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayer,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new[] { ROOM_TITLE_KEY, ROOM_PW_KEY }
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }

    // ===== QuickStart =====

    private void HandleQuickStartRequested()
    {
        Debug.Log("[Lobby] Try QuickStart");

        var expectedProps = new ExitGames.Client.Photon.Hashtable
        {
            { "pw", string.Empty }
        };

        PhotonNetwork.JoinRandomRoom(expectedProps, 0);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Lobby] QuickStart failed: {returnCode} / {message}");

        _ui.ShowNotice("빠른 시작에 실패했습니다. \n 잠시 후 다시 시도해주세요.");
    }

    // ===== Join =====

    private void HandleJoinRequested(RoomSnapshot snap)
    {
        if (!snap.IsValid) return;
        if (!_cachedRoomList.TryGetValue(snap.Name, out RoomInfo info)) return;
        if (!CanJoin(info)) return;

        PhotonNetwork.JoinRoom(snap.Name);
    }

    private void HandlePasswordJoinRequested(RoomSnapshot snap, string inputPw)
    {
        if (_ui == null) return;

        if (!snap.IsValid)
        {
            _ui.ShowJoinPasswordError("방 정보를 찾을 수 없습니다.");
            return;
        }

        if (!_cachedRoomList.TryGetValue(snap.Name, out RoomInfo info))
        {
            _ui.ShowJoinPasswordError("방 정보를 찾을 수 없습니다.");
            return;
        }

        if (!CanJoin(info))
        {
            _ui.ShowJoinPasswordError("현재 입장할 수 없는 방입니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(inputPw))
        {
            _ui.ShowJoinPasswordError("비밀번호를 입력해주세요.");
            return;
        }

        string roomPw = string.Empty;
        if (info.CustomProperties != null &&
            info.CustomProperties.TryGetValue("pw", out object pwObj))
        {
            roomPw = pwObj as string ?? string.Empty;
        }

        if (roomPw != inputPw)
        {
            _ui.ShowJoinPasswordError("비밀번호가 틀렸습니다.");
            return;
        }

        _ui.CloseJoinPassword();
        PhotonNetwork.JoinRoom(snap.Name);
    }

    private bool CanJoin(RoomInfo info)
    {
        if (!info.IsOpen) return false;
        if (info.PlayerCount >= info.MaxPlayers) return false;
        return true;
    }

    // ===== Leave To Title =====

    private void HandleLeaveToTitleRequested()
    {
        Debug.Log("[Lobby] Try to Leave to Title");
        if (_leaveToTitleRequested) return;
        _leaveToTitleRequested = true;

        StopAutoRefresh();
        StartCoroutine(Co_DisconnectAndGoTitle());
    }

    private IEnumerator Co_DisconnectAndGoTitle()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        }

        Debug.Log("[Lobby] DisConnect Complete. Go to Title...");
        SceneManager.LoadScene("Title");
    }

    // ===== Scene =====

    public override void OnJoinedRoom()
    {
        Debug.Log("[Lobby] CB: Joined Room");
        SceneManager.LoadScene("Room");
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[Lobby] CB: Left Lobby -> Go to Title");
        SceneManager.LoadScene("Title");
    }
}
