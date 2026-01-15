using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

public class CreateVoice : MonoBehaviourPun
{
    public void CreateVoicePV(PhotonView requesterPV, Transform parent = null)
    {
        //1.내(Local) 화면에서만 생성 명령 실행
        if (requesterPV.IsMine == false) return;

        GameObject voiceObj = PhotonNetwork.Instantiate("VoicePrefab", Vector3.zero, Quaternion.identity);
        PhotonView voicePV = voiceObj.GetComponent<PhotonView>();
        Speaker speaker = voiceObj.GetComponent<Speaker>();

        PunVoiceClient.Instance.AddSpeaker(speaker, voicePV);

        // 4. 부모 자식 관계 설정 (씬 확장성 핵심)
        if (parent != null && parent.TryGetComponent(out PhotonView parentPV))
        {
            voiceObj.transform.SetParent(parent, false);
            voiceObj.transform.localPosition = Vector3.zero;
            // 이 스크립트(CreateVoice)의 PV를 이용해 RPC 호출
            this.photonView.RPC(nameof(RPC_SyncVoiceParent), RpcTarget.OthersBuffered, voicePV.ViewID, parentPV.ViewID);
        }
    }
    
    [PunRPC]
    public void RPC_SyncVoiceParent(int voiceID, int parentID)
    {
        PhotonView vpv = PhotonView.Find(voiceID);
        PhotonView ppv = PhotonView.Find(parentID);

        if (vpv != null && ppv != null)
        {
            vpv.transform.SetParent(ppv.transform, false);
            vpv.transform.localPosition = Vector3.zero; // 위치 초기화
        }
    }
}