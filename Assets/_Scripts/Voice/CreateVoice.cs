using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

public class CreateVoice : MonoBehaviourPun
{
    public void CreateVoicePV(PhotonView requesterPV, Transform parent = null)
    {
        if (!requesterPV.IsMine) return;

        // 부모의 ViewID를 데이터에 담습니다 (부모가 없으면 0)
        object[] initData = new object[1];
        initData[0] = (parent != null) ? parent.GetComponent<PhotonView>().ViewID : 0;

        // Instantiate 시 데이터를 함께 보냅니다.
        PhotonNetwork.Instantiate("VoicePrefab", Vector3.zero, Quaternion.identity, 0, initData);
    }
}