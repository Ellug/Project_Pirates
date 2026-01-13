using UnityEngine;
using Photon.Pun;

public class PhotonPunManager : Singleton<PhotonPunManager>
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
}
