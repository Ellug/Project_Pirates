using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System;

public class RaiseEventManager : MonoBehaviour, IOnEventCallback
{
    //ex) RaiseEventManager.Instance.Raise(RaiseEventCode.blackout, null, SendOptions.SendReliable, ReceiverGroup.All);
    // RaiseEventManager.Instance.Raise(enum쪽 코드, Null(데이터), 옵션, 받는 사람 목록) 형태로 사용
    // 샌드옵션의 SendReliable = 전송 보장 SendUnreliable = 빠르지만 소실 가능성 있음
    // ReceiverGroup.All 나 포함 전원
    // ReceiverGroup.Others 나 제외 전원
    // ReceiverGroup.MasterClient 마스터만

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void Raise(RaiseEventCode code, object data, SendOptions sendOptions, ReceiverGroup receivers = ReceiverGroup.All)
    {
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = receivers
        };

        PhotonNetwork.RaiseEvent((byte)code, data, options, sendOptions);
    }
    public void OnEvent(EventData RaiseEvent) 
    {
        RaiseEventCode code = (RaiseEventCode)RaiseEvent.Code;
        object data = RaiseEvent.CustomData;

        switch (code) 
        {
            case RaiseEventCode.blackout:
                OnBlackout?.Invoke();
                break;
            default:
                break;
        }
    }

    //추가할 이벤트 등록해 놓을것

    public event Action OnBlackout;
}

