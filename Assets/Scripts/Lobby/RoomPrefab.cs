using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RoomPrefab : MonoBehaviourPun
{
    [SerializeField] private TextMeshProUGUI _roomTitleText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private RawImage _lockedImg;
    [SerializeField] private TMP_InputField _roomPWInput;

    public static Action<RoomInfo, string> OnTryJoinRoom;

    private RoomInfo _roomInfo;

    private void Awake()
    {
        _roomPWInput.onSubmit.AddListener(PWInputEnter);
    }

    public void Init(RoomInfo roomInfo)
    {
        _roomInfo = roomInfo;
        Debug.Log($"RoomPrefab Init: {_roomInfo?.Name}");

        _roomTitleText.text = roomInfo.Name;
        _playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

        bool isPrivate =
            roomInfo.CustomProperties.ContainsKey("pw") &&
            !string.IsNullOrEmpty(roomInfo.CustomProperties["pw"] as string);

        _lockedImg.gameObject.SetActive(isPrivate);
        _roomPWInput.gameObject.SetActive(isPrivate);
    }

    public void OnClickRoomPrefab()
    {
        if (_roomInfo == null)
        {
            Debug.LogWarning("RoomInfo가 아직 설정되지 않았습니다!");
            return;
        }
        OnTryJoinRoom?.Invoke(_roomInfo, _roomPWInput.text ?? string.Empty);
    }

    private void PWInputEnter(string input)
    {
        if (_roomInfo == null) return;
        if (string.IsNullOrEmpty(input)) return;

        OnTryJoinRoom?.Invoke(_roomInfo, input);
    }
}
