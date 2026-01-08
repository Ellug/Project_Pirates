using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviourPunCallbacks
{
    [Header("룸 리스트")]
    [SerializeField] private Transform _roomListPanel;
    [SerializeField] private TextMeshProUGUI _emptyText;
    [SerializeField] private GameObject _roomPrefab;

    [Header("유저 인포")]
    [SerializeField] TextMeshProUGUI nickName;

    [Header("방 만들기")]
    [SerializeField] private GameObject _makeRoomPanel;

    private Dictionary<string, RoomInfo> _cachedRoomList = new();
    public static event Action<string, string, int> OnCreateRoomRequest;
    

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InLobby);
        _makeRoomPanel.SetActive(false);
        nickName.text = PhotonNetwork.NickName;
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

    private void RefreshRoomUI()
    {
        foreach (Transform child in _roomListPanel)
        {
            Destroy(child.gameObject);
        }

        if (_cachedRoomList.Count == 0)
        {
            _emptyText.text = "방이 없습니다...";
            return;
        }

        _emptyText.text = string.Empty;

        foreach (var roomInfo in _cachedRoomList.Values)
        {
            var room = Instantiate(_roomPrefab, _roomListPanel);

            room.transform.Find("Title")
                .GetComponentInChildren<TextMeshProUGUI>()
                .text = roomInfo.Name;

            room.transform.Find("Count")
                .GetComponentInChildren<TextMeshProUGUI>()
                .text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

            var lockImg = room.GetComponentInChildren<RawImage>();
            var roomPW = room.GetComponentInChildren<TMP_InputField>();

            bool isPrivate = roomInfo.CustomProperties.ContainsKey("pw")
              && !string.IsNullOrEmpty(
                     roomInfo.CustomProperties["pw"] as string
                 );

            lockImg.gameObject.SetActive(isPrivate);
            roomPW.gameObject.SetActive(isPrivate);
        }
    }

    public void OnClickRefresh()
    {
        RefreshRoomUI();
    }

    public void OnClickCreateRoom()
    {
        _makeRoomPanel.SetActive(true);
    }

    public void OnClickApplyButton()
    {
        string _roomName = _makeRoomPanel.transform.Find("RoomName").GetComponent<TMP_InputField>().text;
        string _roomPW = _makeRoomPanel.transform.Find("RoomPW").GetComponent<TMP_InputField>().text;
        int _playerMaxCount = _makeRoomPanel.transform.Find("PlayerCount").GetComponent<TMP_Dropdown>().value;

        OnCreateRoomRequest?.Invoke(_roomName, _roomPW, _playerMaxCount);

        _makeRoomPanel.SetActive(false);
    }

    public void OnClickCancleButton()
    {
        _makeRoomPanel.SetActive(false);
    }
}
