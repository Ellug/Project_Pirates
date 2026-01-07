using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonPunManager : Singleton<PhotonPunManager>, IConnectionCallbacks, IMatchmakingCallbacks
{
    [Header("Photon Global Config")]
    [SerializeField] private string _gameVersion = "prototype";
    [SerializeField] private string _fixedRegion = "kr";
    [SerializeField] private bool _autoSyncScene = true;

    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsReady => PhotonNetwork.IsConnectedAndReady;
    public bool InLobby => PhotonNetwork.InLobby;
    public bool InRoom => PhotonNetwork.InRoom;

    protected override void OnSingletonAwake()
    {
        Debug.Log("[PUN] Init Global Photon Config");

        PhotonNetwork.GameVersion = _gameVersion;
        PhotonNetwork.AutomaticallySyncScene = _autoSyncScene;

        if (PhotonNetwork.PhotonServerSettings != null)
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = _fixedRegion;

        PhotonNetwork.RemoveCallbackTarget(this);
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDestroy()
    {
        if (Instance == this)
            PhotonNetwork.RemoveCallbackTarget(this);
    }

    // CB
    public void OnConnected()
    {
        Debug.Log("[PUN][CB] OnConnected");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("[PUN][CB] OnConnectedToMaster");
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[PUN][CB] OnDisconnected | {cause}");
    }

    public void OnJoinedLobby()
    {
        Debug.Log("[PUN][CB] OnJoinedLobby");
    }

    public void OnLeftLobby()
    {
        Debug.Log("[PUN][CB] OnLeftLobby");
    }

    public void OnCreatedRoom()
    {
        Debug.Log("[PUN][CB] OnCreatedRoom");
    }

    public void OnJoinedRoom()
    {
        Debug.Log($"[PUN][CB] OnJoinedRoom | {PhotonNetwork.CurrentRoom?.Name}");
    }

    public void OnLeftRoom()
    {
        Debug.Log("[PUN][CB] OnLeftRoom");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PUN][CB] CreateRoomFailed | {returnCode} {message}");
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PUN][CB] JoinRoomFailed | {returnCode} {message}");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PUN][CB] JoinRandomFailed | {returnCode} {message}");
    }

    // Unused
    public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friendList) { }
    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
}
