using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private RoomPlayerListView _playerListView;
    [SerializeField] private Button _startButton;

    private readonly RoomReadyStateCheck _ready = new();
    private Player[] _cache = new Player[16];

    // 키 나중에 버튼 도입하고 지워
    Keyboard key = Keyboard.current;

    void Start()
    {
        Debug.Log($"[Room] Start. startButtonAssigned={_startButton != null}, listViewAssigned={_playerListView != null && _playerListView.HasText}");
        GameManager.Instance.SetSceneState(SceneState.Room);

        _ready.SetLocalReady(false);

        if (_startButton != null)
        {
            _startButton.gameObject.SetActive(false);
            _startButton.onClick.RemoveListener(OnClickStartGame);
            _startButton.onClick.AddListener(OnClickStartGame);
        }

        RefreshRoomUI("Start");
    }

    void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnClickStartGame);
    }

    // 임시. 나중에 버틴 도입하고 지워 변경
    void Update()
    {        
        if (key == null) return;

        if (key.lKey.wasPressedThisFrame)
            LeaveRoom();

        if (key.rKey.wasPressedThisFrame)
            ToggleReady();
    }

    // ToggleReady
    public void ToggleReady()
    {
        _ready.ToggleLocalReady();
        RefreshRoomUI("ToggleReady");        
    }

    private int Refresh()
    {
        var list = PhotonNetwork.PlayerList;
        int count = (list != null) ? list.Length : 0;

        if (count <= 0)
            return 0;

        for (int i = 0; i < count; i++)
            _cache[i] = list[i];

        return count;
    }

    private void RefreshRoomUI(string reason)
    {
        if (!PhotonNetwork.InRoom)
        {
            if (_playerListView != null) _playerListView.SetNotInRoom();
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            return;
        }

        int count = Refresh();
        var players = _cache;

        if (_playerListView != null)
            _playerListView.Render(PhotonNetwork.CurrentRoom?.Name, players, count, PhotonNetwork.LocalPlayer);

        RefreshStartButton(players, count);

        Debug.Log($"[Room] UI Refresh ({reason}) players={count}");
    }

    private void RefreshStartButton(Player[] players, int count)
    {
        if (_startButton == null) return;

        if (!PhotonNetwork.IsMasterClient) // 방장 아니면 스타트 버튼 안보임
        {
            _startButton.gameObject.SetActive(false);
            return;
        }

        // 전부 레디하면 스타트 버튼 보이고 인터랙터블 킴
        bool show = _ready.AreAllPlayersReady(players, count);
        _startButton.gameObject.SetActive(show);
        _startButton.interactable = show;
    }

    private void OnClickStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int count = Refresh();
        var players = _cache;

        if (!_ready.AreAllPlayersReady(players, count)) return;

        Debug.Log("[Room] Start Game → LoadLevel(InGame) for all");
        PhotonNetwork.LoadLevel("InGame");
    }

    // LeaveRoom
    public void LeaveRoom()
    {
        Debug.Log("[Room] L pressed → Room Out");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[Room] OnLeftRoom -> Go to Lobby");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshRoomUI("OnPlayerEnteredRoom");
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshRoomUI("OnPlayerLeftRoom");

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // ready 변경 포함이면 갱신
        if (_ready.IsReadyChanged(changedProps))
            RefreshRoomUI("OnPlayerPropertiesUpdate:ready");
        else
            RefreshRoomUI("OnPlayerPropertiesUpdate");
    }

    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshRoomUI("OnMasterClientSwitched");
}
