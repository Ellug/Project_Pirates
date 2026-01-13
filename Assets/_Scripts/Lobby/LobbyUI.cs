using Photon.Pun;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviourPunCallbacks
{
    [Header("룸 리스트")]
    [SerializeField] private Transform _roomListPanel;
    [SerializeField] private GameObject _roomPrefab;
    [SerializeField] private TextMeshProUGUI _emptyText;

    [Header("유저 인포")]
    [SerializeField] private TextMeshProUGUI nickName;

    [Header("방 만들기")]
    [SerializeField] private GameObject _makeRoomPanel;
    [SerializeField] private TMP_InputField _makeRoomTitle;
    [SerializeField] private TMP_InputField _makeRoomPW;
    [SerializeField] private TMP_Dropdown _makeRoomPlayerCount;

    public static event Action<string, string, int> OnCreateRoomRequest;
    public static event Action OnRefreshRoomListRequest;
    
    public Transform RoomListPanel => _roomListPanel;
    public GameObject RoomPrefab => _roomPrefab;
    public TextMeshProUGUI EmptyText => _emptyText;


    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InLobby);
        _makeRoomPanel.SetActive(false);
        nickName.text = PhotonNetwork.NickName;
        OnRefreshRoomListRequest?.Invoke();
    }

    public void OnClickRefresh()
    {
        OnRefreshRoomListRequest?.Invoke();
    }

    public void OnClickCreateRoom()
    {
        _makeRoomPanel.SetActive(true);
    }

    public void OnClickApplyButton()
    {
        OnCreateRoomRequest?.Invoke(_makeRoomTitle.text, _makeRoomPW.text, _makeRoomPlayerCount.value + 1); //이벤트 호출

        _makeRoomPanel.SetActive(false);
    }

    public void OnClickCancleButton()
    {
        _makeRoomPanel.SetActive(false);
    }
}
