using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public sealed class RoomUI: MonoBehaviour
{
    private const string PROP_READY = "ready";

    [Header("Header")]
    [SerializeField] private TMP_Text _headerRoomName;
    [SerializeField] private Button _headerRoomSetting;
    [SerializeField] private GameObject _headerRoomLocked;

    [Header("Room Setting Panel")]
    [SerializeField] private RoomSettingsPanelView _roomSettingsPanel;

    [Header("Player Content Prefab")]
    [SerializeField] private RoomPlayerContentView _playerContentPrefab;

    [Header("Player Content Root (Container)")]
    [SerializeField] private Transform _playerContentRoot; // 그리드/레이아웃이 붙은 컨테이너

    [Header("Style")]
    [SerializeField] private string _meNickColorHtml = "#c3ec22";
    [SerializeField] private string _readyOnColorHtml = "#00C853";
    [SerializeField] private string _readyOffColorHtml = "#B0B0B0";

    private readonly List<RoomPlayerContentView> _items = new();

    private Color _meNickColor;
    private Color _readyOnColor;
    private Color _readyOffColor;

    void Awake()
    {
        _meNickColor = ParseHtmlOrFallback(_meNickColorHtml, Color.red);
        _readyOnColor = ParseHtmlOrFallback(_readyOnColorHtml, Color.green);
        _readyOffColor = ParseHtmlOrFallback(_readyOffColorHtml, new Color(0.7f, 0.7f, 0.7f, 1f));

        if (_headerRoomSetting != null)
            _headerRoomSetting.onClick.AddListener(OpenRoomSettingsPanel);
    }

    void OnDestroy()
    {
        if (_headerRoomSetting != null)
            _headerRoomSetting.onClick.RemoveListener(OpenRoomSettingsPanel);
    }

    // 외부(RoomManager)에서 호출 - 방 이름/잠금 상태 갱신 & 컨테이너에 플레이어 프리팹으로 목록 렌더
    public void Render(string roomName, bool hasPassword, Player[] players, int count, Player localPlayer)
    {
        RenderHeader(roomName, hasPassword);

        if (_playerContentPrefab == null || _playerContentRoot == null)
            return;

        EnsureItemCount(count);

        int masterActorNumber = PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null
            ? PhotonNetwork.MasterClient.ActorNumber
            : -1;

        for (int i = 0; i < count; i++)
        {
            var p = players[i];
            var view = _items[i];

            bool isMe = (p != null && localPlayer != null && p.ActorNumber == localPlayer.ActorNumber);
            bool isMaster = (p != null && p.ActorNumber == masterActorNumber);
            bool ready = IsReady(p);

            // 킥 버튼: 로컬이 마스터이고, 대상이 나 자신이 아닐 때만 노출 (필요 시 정책 변경)
            bool kickEnabled = PhotonNetwork.InRoom
                               && PhotonNetwork.IsMasterClient
                               && p != null
                               && localPlayer != null
                               && p.ActorNumber != localPlayer.ActorNumber;

            view.gameObject.SetActive(true);
            view.Bind(
                player: p,
                isMe: isMe,
                isMaster: isMaster,
                isReady: ready,
                meNickColor: _meNickColor,
                readyOnColor: _readyOnColor,
                readyOffColor: _readyOffColor,
                kickEnabled: kickEnabled,
                onKick: kickEnabled ? HandleKickRequested : null
            );
        }

        // 남는 뷰 비활성화
        for (int i = count; i < _items.Count; i++)
        {
            _items[i].Unbind();
            _items[i].gameObject.SetActive(false);
        }
    }

    private void RenderHeader(string roomName, bool hasPassword)
    {
        if (_headerRoomName != null)
            _headerRoomName.text = roomName ?? string.Empty;

        if (_headerRoomLocked != null)
            _headerRoomLocked.SetActive(hasPassword);

        // 방 마스터만 방 세팅 버튼
        if (_headerRoomSetting != null)
            _headerRoomSetting.interactable = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
    }

    private void EnsureItemCount(int needed)
    {
        while (_items.Count < needed)
        {
            var inst = Instantiate(_playerContentPrefab, _playerContentRoot);
            inst.gameObject.SetActive(false);
            _items.Add(inst);
        }
    }

    private void OpenRoomSettingsPanel()
    {
        // 패널은 아직 미구현이므로, 베이스만 열어둠
        if (_roomSettingsPanel != null)
            _roomSettingsPanel.Open();
    }

    // 강퇴 -> RoomPlayerContentView 온 킥에 연결
    private void HandleKickRequested(Player target)
    {
        if (target == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var options = new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } };
        PhotonNetwork.RaiseEvent(RoomManager.KickEventCode, target.ActorNumber, options, SendOptions.SendReliable);
    }

    private static bool IsReady(Player p)
    {
        if (p == null || p.CustomProperties == null) return false;
        if (p.CustomProperties.TryGetValue(PROP_READY, out object v) && v is bool b) return b;
        return false;
    }

    private static Color ParseHtmlOrFallback(string html, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(html) && ColorUtility.TryParseHtmlString(html, out var c))
            return c;
        return fallback;
    }
}
