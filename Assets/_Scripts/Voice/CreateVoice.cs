using Photon.Pun;
using Photon.Voice.PUN;
using System.Collections;
using UnityEngine;

public class CreateVoice : MonoBehaviourPun
{
    public IEnumerator CreateVoicePV(PhotonView requesterPV = null, Transform parent = null)
    {
        if (requesterPV != null && !requesterPV.IsMine) yield break;

        // 1. 부모 ViewID 확인
        PhotonView parentPV = parent != null ? parent.GetComponent<PhotonView>() : null;
        if (parentPV != null)
        {
            yield return new WaitUntil(() => parentPV.ViewID > 0);
        }

        object[] initData = new object[1];
        initData[0] = (parentPV != null) ? parentPV.ViewID : 0;

        GameObject voiceObj = PhotonNetwork.Instantiate("VoicePrefab", Vector3.zero, Quaternion.identity, 0, initData);

        // 3. PhotonVoiceView 및 Recorder 상태 체크
        PhotonVoiceView voiceView = voiceObj.GetComponent<PhotonVoiceView>();

        if (voiceView != null)
        {
            yield return new WaitUntil(() => voiceView.RecorderInUse != null);

            yield return new WaitUntil(() => voiceView.isActiveAndEnabled);

            if (voiceView.RecorderInUse.TransmitEnabled)
            {
                yield return new WaitUntil(() => voiceView.IsRecording);
            }
        }
        yield return new WaitForFixedUpdate();
    }
}