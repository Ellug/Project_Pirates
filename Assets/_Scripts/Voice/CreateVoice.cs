using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

public class CreateVoice : MonoBehaviourPun
{
    public void CreateVoicePV(Photon.Realtime.Player player, Transform parent = null)
    {

        // 씬 내 위치에 VoicePrefab 생성
        GameObject voiceObj = PhotonNetwork.Instantiate("VoicePrefab", Vector3.zero, Quaternion.identity);
        
        if (parent != null)
            voiceObj.transform.SetParent(parent, false);

        Speaker speaker = voiceObj.GetComponent<Speaker>();
        PhotonView pv = voiceObj.GetComponent<PhotonView>();

        // 로컬 플레이어만
        if (player != PhotonNetwork.LocalPlayer) return; 

        // userData로 PhotonView 전달
        PunVoiceClient.Instance.AddSpeaker(speaker, pv);
    }
}