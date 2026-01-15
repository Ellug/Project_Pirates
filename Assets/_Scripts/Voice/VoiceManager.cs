using Photon;
using Photon.Pun;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

public class VoiceManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Recorder recorder;
    [SerializeField] private Speaker speaker;

    private void Start()
    {
        if (recorder == null || speaker == null)
        {
            Debug.LogError("Recorder 또는 Speaker가 없습니다.");
            return;
        }

        recorder.DebugEchoMode = true;
        recorder.TransmitEnabled = true;

        Debug.Log("Photon Voice Debug Echo 활성화됨");
    }
}
