using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

// RPC로 방 전체 전달
public class ChatNetwork : MonoBehaviourPun
{
    [SerializeField] private ChatLogView _logView;
    //[SerializeField] private int _maxChars = 80;

    // ChatInput 에서 호출
    public void SendChat(string rawText)
    {
        if (!PhotonNetwork.InRoom) return;

        string text = (rawText ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return;

        string nickName = PhotonNetwork.LocalPlayer?.NickName;
        if (string.IsNullOrEmpty(nickName))
            nickName = PhotonNetwork.LocalPlayer?.UserId ?? "Unknown";

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        //photonView.RPC()
    }
}
