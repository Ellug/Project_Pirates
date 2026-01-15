using Photon.Pun;
using UnityEngine;

// RPC로 방 전체 전달
public class ChatNetwork : MonoBehaviourPun
{
    [SerializeField] private ChatLogView _logView;
    private PhotonView _photonView;

    // ChatInput 에서 호출
    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    public void SendChat(string rawText)
    {
        if (!PhotonNetwork.InRoom) return;

        string text = (rawText ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return;

        string nickName = PhotonNetwork.LocalPlayer?.NickName;
        if (string.IsNullOrEmpty(nickName))
            nickName = PhotonNetwork.LocalPlayer?.UserId ?? "Unknown";

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        _photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, actorNumber, nickName, text);
    }

    [PunRPC]
    private void RPC_ReceiveChat(int sendActorNumber, string sendNickname, string text)
    {
        if (_logView == null) return;

        bool isMine = sendActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isMine)
        {
            _logView.AddMessage(
                $"<color=#FFD400>[ {sendNickname} ] : {text}</color>"
            );
        }
        else
        {
            _logView.AddMessage(
                $"[ {sendNickname} ] : {text}"
            );
        }
    }
}
