using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

public class VoiceLinker : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // 포톤이 오브젝트를 생성할 때 자동으로 호출하는 콜백
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data == null || data.Length == 0) return;

        int parentViewID = (int)data[0];

        // 0이 아니라면 부모를 찾아서 붙음
        if (parentViewID != 0)
        {
            PhotonView parentPV = PhotonView.Find(parentViewID);
            if (parentPV != null)
            {
                transform.SetParent(parentPV.transform, false);
                transform.localPosition = Vector3.zero;

                if (!info.photonView.IsMine)
                {

                    if (VoiceManager.Instance != null)
                    {
                        VoiceManager.Instance.ApplyMasterOutputSettings();
                    }
                }
            }
        }

        Speaker speaker = GetComponent<Speaker>();
        if (speaker != null && VoiceManager.Instance != null)
        {
            VoiceManager.Instance.OnSpeakerCreated(speaker);
            Debug.Log($"[Linker] {parentViewID} Speaker 탐색완료");
        }
    }
}