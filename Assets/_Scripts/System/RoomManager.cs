using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RoomManager : MonoBehaviourPunCallbacks
{
    private const string READY_KEY = "ready";

    [Header("UI")]
    [SerializeField] private RoomPlayerListView _playerListView;
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _startButtonText;

    [Header("Chat Log View")]
    [SerializeField] private ChatLogView _roomLogView;

    private readonly RoomReadyStateCheck _ready = new();
    private Player[] _cache = new Player[16];

    void Start()
    {
        Debug.Log($"[Room] Start. startButtonAssigned={_startButton != null}, listViewAssigned={_playerListView != null && _playerListView.HasText}");
        
        GameManager.Instance.SetSceneState(SceneState.Room);

        _ready.SetLocalReady(false);        

        // 방 진입 후 내 상태 출력
        if(PhotonNetwork.InRoom)
        {
            string roomName = PhotonNetwork.CurrentRoom?.Name ?? "Unknown";
            LogRoom($"[Room] {roomName} 방에 참여 했습니다. [MasterClient] : {PhotonNetwork.MasterClient?.NickName}");
        }

        RefreshRoomUI("Start");
    }

    // 룸 로그 출력
    private void LogRoom(string text)
    {
        Debug.Log(text);

        // 채팅 UI 에 같이 출력
        if(_roomLogView != null)
            _roomLogView.AddMessage(text);
    }

    // 스타트or레디 버튼 클릭
    public void OnClickStartButton()
    {
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient)
            OnClickStartGame();
        else
            ToggleReady();
    }

    // ToggleReady
    public void ToggleReady()
    {
        if (!PhotonNetwork.InRoom) return;

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


    // 룸 UI 새로고침 
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

    
    // 메인 버튼(방장: GameStart / 비방장: Ready 토글)
    private void RefreshStartButton(Player[] players, int count)
    {
        if (_startButton == null) return;

        _startButton.gameObject.SetActive(true);

        if (PhotonNetwork.IsMasterClient)
        {
            SetStartButtonText("GameStart");

            bool canStart = CanMasterStart(players, count);
            _startButton.interactable = canStart;
            return;
        }

        bool localReady = IsPlayerReady(PhotonNetwork.LocalPlayer);
        SetStartButtonText(localReady ? "UnReady" : "Ready");
        _startButton.interactable = true;
    }

    // 방장 시작 가능 조건: 방장 제외 전원 레디 (혼자 방이면 true)
    private bool CanMasterStart(Player[] players, int count)
    {
        if (players == null || count <= 0) return false;

        Player master = PhotonNetwork.MasterClient;

        for (int i = 0; i < count; i++)
        {
            var p = players[i];
            if (p == null) continue;

            if (master != null && p.ActorNumber == master.ActorNumber)
                continue;

            if (!IsPlayerReady(p))                
                return false;
        }

        return true;
    }

    // 방장이 게임 시작 누를 시
    private void OnClickStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int count = Refresh();
        var players = _cache;

        if (!CanMasterStart(players, count)) return;
        LogRoom("[Room] All Player is Ready. Game Start!");

        PhotonNetwork.LoadLevel("InGame");
    }

    private static string GetDisplayName(Player p)
    {
        if (p == null) return "Unknown";
        if (!string.IsNullOrEmpty(p.NickName)) return p.NickName;
        if (!string.IsNullOrEmpty(p.UserId)) return p.UserId;
        return $"Actor#{p.ActorNumber}";
    }

    private static bool IsPlayerReady(Player p)
    {
        if (p == null) return false;

        var props = p.CustomProperties;
        if (props != null && props.TryGetValue(READY_KEY, out object v) && v is bool b)
            return b;

        return false;
    }

    private void SetStartButtonText(string value)
    {
        if (_startButtonText != null)
            _startButtonText.text = value;
    }

    // LeaveRoom
    public void LeaveRoom()
    {
        Debug.Log("[Room] Exit Button pressed → Room Out");
        PhotonNetwork.LeaveRoom();
    }

    // CB
    public override void OnLeftRoom()
    {
        Debug.Log("[Room] CB : OnLeftRoom -> Go to Lobby");
        SceneManager.LoadScene("Lobby");
    }

    // 입장 감지 & 출력
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string name = string.IsNullOrEmpty(newPlayer.NickName) ? newPlayer.UserId : newPlayer.NickName;
        LogRoom($"[Room] {name} 님이 입장 했습니다.");
        RefreshRoomUI("OnPlayerEnteredRoom");
    }

    // 퇴장 감지 & 출력
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        bool wasMaster = (PhotonNetwork.MasterClient != null && otherPlayer.ActorNumber == PhotonNetwork.MasterClient.ActorNumber);
        string name = string.IsNullOrEmpty(otherPlayer.NickName) ? otherPlayer.UserId : otherPlayer.NickName;
        LogRoom($"[Room] {name} 님이 떠났습니다. " + (wasMaster ? " (방장이 떠났습니다.)" : ""));
        RefreshRoomUI("OnPlayerLeftRoom");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (_ready.IsReadyChanged(changedProps))
        {
            if (targetPlayer != null)
            {
                bool readyNow = IsPlayerReady(targetPlayer);
                string name = GetDisplayName(targetPlayer);
                LogRoom($"[Room] {name} 님이 {(readyNow ? "Ready" : "Ready 해제")} 했습니다.");
            }

            RefreshRoomUI("OnPlayerPropertiesUpdate:ready");
            return;
        }

        RefreshRoomUI("OnPlayerPropertiesUpdate");
    }

    // 방장 승계 감지 & 출력
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        string name = string.IsNullOrEmpty(newMasterClient.NickName) ? newMasterClient.UserId : newMasterClient.NickName;
        LogRoom($"[Room] {name} 님이 방장이 되셨습니다.");
        RefreshRoomUI("OnMasterClientSwitched");
    }
}
