using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public sealed class RoomManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const string ROOM_TITLE_KEY = "title";
    private const string ROOM_PW_KEY = "pw";
    private const string READY_KEY = "ready";
    private const int MIN_PLAYERS = 1;
    private const int MAX_PLAYERS_LIMIT = 8;
    public const byte KickEventCode = 101;

    [Header("UI")]
    [SerializeField] private RoomUI _roomUI;
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _startButtonText;

    [Header("Chat Log View")]
    [SerializeField] private ChatLogView _roomLogView;

    [Header("Create Voice Prefab")]
    [SerializeField] private CreateVoice _createVoice;

    private readonly RoomReadyStateCheck _ready = new();
    private Player[] _cache = new Player[16];
    private readonly HashSet<int> _readyFirstUpdate = new();

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }   

    void Start()
    {
        Debug.Log($"[Room] Start. startButtonAssigned={_startButton != null}");
        
        GameManager.Instance.SetSceneState(SceneState.Room);

        _ready.SetLocalReady(false);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.CurrentRoom.IsOpen = true;

        if (_roomUI != null)
            _roomUI.RoomSettingsApplyRequested += HandleRoomSettingsApplyRequested;

        // 방 진입 후 내 상태 출력
        StartCoroutine(CoWaitRoomThenRefresh());
    }

    private void OnDestroy()
    {
        if (_roomUI != null)
            _roomUI.RoomSettingsApplyRequested -= HandleRoomSettingsApplyRequested;
    }

    private IEnumerator CoWaitRoomThenRefresh()
    {
        // 룸 진입 완료까지 기다렸다가 1회 강제 갱신
        while (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            yield return null;

        string roomName = PhotonNetwork.CurrentRoom?.Name ?? "Unknown";
        LogRoom($"[Room] {roomName} 방에 참여 했습니다. [MasterClient] : {PhotonNetwork.MasterClient?.NickName}");
        
        //보이스 프리팹 안전 생성
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(_createVoice.CreateVoicePV());

        RefreshRoomUI("CoWaitRoomThenRefresh");
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
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            OnClickStartGame();
        }            
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
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            return;
        }

        int count = Refresh();
        var players = _cache;

        // Header 전용
        string roomName = GetRoomTitle(PhotonNetwork.CurrentRoom);
        bool hasPassword = GetHasPassword(PhotonNetwork.CurrentRoom);

        if (_roomUI != null)
            _roomUI.Render(roomName, hasPassword, players, count, PhotonNetwork.LocalPlayer);

        RefreshStartButton(players, count);

        Debug.Log($"[Room] UI Refresh ({reason}) players={count}");
    }

    private bool GetHasPassword(Room room)
    {
        if (room == null) return false;

        var props = room.CustomProperties;
        if (props == null) return false;

        // string pw (비어있지 않으면 잠금)
        if (props.TryGetValue(ROOM_PW_KEY, out object v))
        {
            if (v is string s) return !string.IsNullOrWhiteSpace(s);
            if (v is bool b) return b;
            if (v != null) return true; // 값이 존재하면 잠금으로 간주
        }

        return false;
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

        PlayerManager.Instance.StartGameInit(count);
        PhotonNetwork.LoadLevel("InGameLoading");
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

    private void ReadyCallBack(Action callback) 
    {
        _ready.SetLocalReady(false);
        callback?.Invoke();
    }

    public void LeaveRoom()
    {
        Debug.Log("[Room] Triggered LeaveRoom.");
        ReadyCallBack(() => PhotonNetwork.LeaveRoom());
    }

    // CB
    public override void OnLeftRoom()
    {
        if (PlayerManager.Instance != null)
            Destroy(PlayerManager.Instance.gameObject);

        Debug.Log("[Room] CB : OnLeftRoom -> Go to Lobby");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnJoinedRoom() 
    {
        _readyFirstUpdate.Clear();
        _ready.SetLocalReady(false);
        RefreshRoomUI("OnJoinedRoom");
    }

    public void OnEvent(ExitGames.Client.Photon.EventData photonEvent)
    {
        Debug.Log("[Room] OnEvent " + photonEvent.Code);
        if (photonEvent == null) return;
        
        if (photonEvent.Code == KickEventCode)
        {
            if (!PhotonNetwork.InRoom) return;

            if (photonEvent.CustomData is int targetActor && PhotonNetwork.LocalPlayer.ActorNumber == targetActor)
                PhotonNetwork.LeaveRoom();
        }
    }

    // 입장 감지 & 출력
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string name = string.IsNullOrEmpty(newPlayer.NickName) ? newPlayer.UserId : newPlayer.NickName;
        LogRoom($"[Room] {name} is Enter the Room.");
        RefreshRoomUI("OnPlayerEnteredRoom");
    }

    // 퇴장 감지 & 출력
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        bool wasMaster = (PhotonNetwork.MasterClient != null && otherPlayer.ActorNumber == PhotonNetwork.MasterClient.ActorNumber);
        string name = string.IsNullOrEmpty(otherPlayer.NickName) ? otherPlayer.UserId : otherPlayer.NickName;
        LogRoom($"[Room] {name} is left. " + (wasMaster ? " (MasterClient is left.)" : ""));
        RefreshRoomUI("OnPlayerLeftRoom");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (_ready.IsReadyChanged(changedProps))
        {
            if (targetPlayer != null)
            {
                int actor = targetPlayer.ActorNumber;
                bool readyNow = IsPlayerReady(targetPlayer);

                // 이 플레이어의 첫 ready 변경 콜백이면?
                if(_readyFirstUpdate.Add(actor))
                {
                    // 첫 콜백이 false 이면 로그 출력하고 스킵
                    if(!readyNow)
                    {
                        RefreshRoomUI("OnPlayerPropertiesUpdate:ready(first-skip)");
                        return;
                    }
                }
                string name = GetDisplayName(targetPlayer);
                LogRoom($"[Room] {name} 님이 {(readyNow ? "Ready" : "Ready 해제")} 했습니다.");
            }
            RefreshRoomUI("OnPlayerPropertiesUpdate:ready");
            return;
        }
        RefreshRoomUI("OnPlayerPropertiesUpdate");
    }

    // 룸 프로퍼티 바꼇을 때 리프레쉬
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        if(changed == null)
        {
            RefreshRoomUI("OnRoomPropertiesUpdate:null");
            return;
        }

        if(changed.ContainsKey(ROOM_TITLE_KEY) ||
           changed.ContainsKey(ROOM_PW_KEY))
        {
            RefreshRoomUI("OnRoomPropertiesUpdate:settings");
            return;
        }
        RefreshRoomUI("OnRoomPropertiesUpdate");
    }

    // 방장 승계 감지 & 출력
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        string name = string.IsNullOrEmpty(newMasterClient.NickName) ? newMasterClient.UserId : newMasterClient.NickName;
        LogRoom($"[Room] {name} 님이 방장이 되셨습니다.");

        if (PhotonNetwork.LocalPlayer != null
            && newMasterClient != null
            && PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            _ready.SetLocalReady(false);
        }

        RefreshRoomUI("OnMasterClientSwitched");
    }

    private void HandleRoomSettingsApplyRequested(string title, string pw, int max)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        // 방장만 변경 가능
        if(!PhotonNetwork.IsMasterClient)
        {
            LogRoom("[Room] 방 설정은 방장만 변경할 수 있습니다.");
            return;
        }

        Room room = PhotonNetwork.CurrentRoom;

        // 방제/비밀번호는 커스텀 프로퍼티로 유지
        var ht = new ExitGames.Client.Photon.Hashtable
        {
            [ROOM_TITLE_KEY] = string.IsNullOrWhiteSpace(title) ? "" : title.Trim(),
            [ROOM_PW_KEY] = string.IsNullOrWhiteSpace(pw) ? "" : pw.Trim(),
        };
        room.SetCustomProperties(ht);

        if(max <= 0)
        {
            RefreshRoomUI("ApplyRoomSettings(local:no-max-change)");
            return;
        }

        int clamped = Mathf.Clamp(max, MIN_PLAYERS, MAX_PLAYERS_LIMIT);

        // 현재 인원보다 작게 줄이려는 시도는 거부
        int currentCount = room.PlayerCount;
        if(clamped < currentCount)
        {
            LogRoom($"[Room] 현재 인원({currentCount})보다 작은 MaxPlayers({clamped})로 변경할 수 없습니다.");
            RefreshRoomUI("ApplyRoomSettings(local:reject-max)");
            return;
        }

        // Room.MaxPlayers 타입 byte
        byte newMax = (byte)clamped;
        if(room.MaxPlayers != newMax)
        {
            PhotonNetwork.CurrentRoom.MaxPlayers = newMax;
            LogRoom($"[Room] MaxPlayers 변경: {room.MaxPlayers}");
        }
        roomCapacitySafe("ApplyRoomSettings");

        RefreshRoomUI("ApplyRoomSettings(local)");
    }

    private void roomCapacitySafe(string reason)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        var room = PhotonNetwork.CurrentRoom;
        if(room.PlayerCount > room.MaxPlayers)
        {
            if (PhotonNetwork.IsMasterClient)
                room.IsOpen = false;

            LogRoom($"[Room] 인원 초과 상태 감지: {room.PlayerCount}/{room.MaxPlayers}");
        }
    }

    private string GetRoomTitle(Room room)
    {
        if (room == null) return "";

        var props = room.CustomProperties;
        if (props != null &&
            props.TryGetValue(ROOM_TITLE_KEY, out object value) &&
            value is string str &&
            !string.IsNullOrWhiteSpace(str))
        {
            return str;
        }
        return room.Name ?? "";
    }
}