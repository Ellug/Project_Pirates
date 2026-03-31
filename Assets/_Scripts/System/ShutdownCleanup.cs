using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;

// 앱 종료 시 Photon 네트워크와 Voice 연결을 안전하게 정리하는 유틸리티 클래스.
public static class ShutdownCleanup
{
    // 정리 작업이 이미 시작되었는지 추적하는 플래그
    private static bool _started;

    // 외부에서 정리 작업 시작 여부를 확인할 수 있는 프로퍼티
    public static bool Started => _started;

    // 정리 작업을 시작함. 이미 시작된 경우 중복 실행 방지.
    public static void Begin(string reason)
    {
        // 이미 시작되었으면 중복 실행 방지
        if (_started) return;
        _started = true;

        Debug.Log($"[ShutdownCleanup] Begin ({reason})");

        // 순서대로 정리: 1.녹음 중지 → 2.음성 연결 해제 → 3.Photon 연결 해제
        StopAllRecorders();
        DisconnectVoice();
        DisconnectPhoton();

        Debug.Log("[ShutdownCleanup] Cleanup calls issued");
    }

    // 강제로 정리 작업 실행. _started 플래그와 무관하게 항상 실행됨.
    // 타임아웃 등 긴급 상황에서 사용.
    public static void Force(string reason)
    {
        Debug.Log($"[ShutdownCleanup] Force ({reason})");
        StopAllRecorders();
        DisconnectVoice();
        DisconnectPhoton();
    }

    // Photon 네트워크 연결이 완전히 해제되었는지 확인.
    public static bool IsPhotonClean()
    {
        return !PhotonNetwork.IsConnected && !PhotonNetwork.IsConnectedAndReady;
    }

    // Voice 클라이언트 연결이 완전히 해제되었는지 확인.
    public static bool IsVoiceClean()
    {
        var voice = FindVoiceClient();
        // Voice 클라이언트가 없거나 Client가 null이면 정리 완료로 간주
        if (voice == null || voice.Client == null) return true;
        // 연결되지 않았고 방에도 없어야 정리 완료
        return !voice.Client.IsConnected && !voice.Client.InRoom;
    }

    // 씬에 존재하는 모든 Recorder의 녹음/전송을 중지.
    // 비활성화된 오브젝트의 Recorder도 포함하여 검색.
    private static void StopAllRecorders()
    {
        // 비활성화된 오브젝트 포함하여 모든 Recorder 검색
        var recorders = Object.FindObjectsByType<Recorder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (recorders == null || recorders.Length == 0) return;

        foreach (var recorder in recorders)
        {
            if (recorder == null) continue;
            // 전송과 녹음 모두 비활성화
            recorder.TransmitEnabled = false;
            recorder.RecordingEnabled = false;
        }
    }

    // Voice 클라이언트의 연결을 해제.
    // 자동 재연결을 방지하기 위해 AutoConnectAndJoin도 false로 설정.
    private static void DisconnectVoice()
    {
        var voice = FindVoiceClient();
        if (voice == null) return;

        // 자동 재연결 방지 후 연결 해제
        voice.AutoConnectAndJoin = false;
        voice.Disconnect();
    }

    // Photon 네트워크 연결을 해제.
    private static void DisconnectPhoton()
    {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();
    }

    // 씬에서 PunVoiceClient를 찾아 반환. (비활성화된 오브젝트 포함)
    private static PunVoiceClient FindVoiceClient()
    {
        return Object.FindFirstObjectByType<PunVoiceClient>(FindObjectsInactive.Include);
    }
}
