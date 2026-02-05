#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;

// 앱 종료 시 Photon/Voice 상태를 진단하고 안전하게 정리하는 디버깅용 컴포넌트.
// 개발 빌드와 에디터에서만 동작하며, 종료 전 네트워크 상태를 로그로 출력함.
public sealed class ShutdownDiagnostics : MonoBehaviour
{
    // 강제 종료까지 대기하는 최대 시간 (초)
    private const float ForceQuitTimeoutSeconds = 3f;

    // 싱글톤 인스턴스
    private static ShutdownDiagnostics _instance;

    // 종료 요청이 들어왔는지 여부
    private bool _quitRequested;

    // 정리 완료 후 실제 종료를 허용할지 여부
    private bool _allowQuit;

    // 종료 요청이 시작된 시간 (타임아웃 계산용)
    private float _quitStart;

    // 씬 로드 후 자동으로 호출되어 싱글톤 인스턴스를 생성.
    // DontDestroyOnLoad로 씬 전환 시에도 유지됨.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        // 새 게임오브젝트 생성 및 컴포넌트 추가
        var go = new GameObject("ShutdownDiagnostics");
        DontDestroyOnLoad(go);  // 씬 전환 시에도 파괴되지 않음
        _instance = go.AddComponent<ShutdownDiagnostics>();
    }

    void Awake()
    {
        // 앱 종료 시도 이벤트에 핸들러 등록
        Application.wantsToQuit += OnWantsToQuit;
    }

    void OnDestroy()
    {
        // 이벤트 핸들러 해제 (메모리 누수 방지)
        Application.wantsToQuit -= OnWantsToQuit;
        Log("OnDestroy");
    }

    private void OnApplicationQuit()
    {
        Log("OnApplicationQuit");
    }

    // 사용자가 앱 종료를 시도할 때 호출됨.
    // false를 반환하면 종료를 지연시키고, true를 반환하면 종료 진행.
    private bool OnWantsToQuit()
    {
        // 현재 네트워크 상태를 로그에 출력
        LogState("wantsToQuit");

#if UNITY_EDITOR
        // 에디터에서는 정리 과정 없이 즉시 종료 허용
        if (Application.isEditor)
            return true;
#endif

        // 정리 완료 후 종료 허용된 상태면 종료 진행
        if (_allowQuit) return true;

        // 이미 종료 요청 처리 중이면 대기 (종료 지연)
        if (_quitRequested) return false;

        // 첫 종료 요청: 정리 코루틴 시작
        _quitRequested = true;
        _quitStart = Time.realtimeSinceStartup;
        StartCoroutine(CoCleanupThenQuit());
        return false;  // 정리가 끝날 때까지 종료 지연
    }

    // 네트워크 정리 후 앱을 종료하는 코루틴.
    // 타임아웃(3초) 내에 정리되지 않으면 강제 정리 후 종료.
    private IEnumerator CoCleanupThenQuit()
    {
        Log("Cleanup begin");

        // ShutdownCleanup을 통해 정리 작업 시작
        ShutdownCleanup.Begin("ShutdownDiagnostics");

        // 타임아웃까지 대기하면서 정리 완료 확인
        float deadline = _quitStart + ForceQuitTimeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            // Photon과 Voice 모두 정리되면 루프 탈출
            if (ShutdownCleanup.IsPhotonClean() && ShutdownCleanup.IsVoiceClean())
                break;
            yield return null;  // 다음 프레임까지 대기
        }

        // 타임아웃 후에도 정리가 안 됐으면 강제 정리
        if (!ShutdownCleanup.IsPhotonClean() || !ShutdownCleanup.IsVoiceClean())
            ShutdownCleanup.Force("ShutdownDiagnostics.Timeout");

        Log("Cleanup end");

        // 정리 완료, 종료 허용 후 앱 종료
        _allowQuit = true;
        Application.Quit();
    }

    // 현재 Photon, Voice, Recorder 상태를 상세하게 로그에 출력.
    // 디버깅 시 네트워크 상태 파악에 유용.
    private void LogState(string tag)
    {
        // Photon 네트워크 상태 수집
        ClientState photonState = PhotonNetwork.NetworkClientState;
        bool photonConnected = PhotonNetwork.IsConnected;
        bool photonInRoom = PhotonNetwork.InRoom;

        // Voice 클라이언트 상태 수집
        var voice = Object.FindFirstObjectByType<PunVoiceClient>(FindObjectsInactive.Include);
        string voiceState = "none";
        if (voice != null && voice.Client != null)
        {
            voiceState = $"{voice.Client.State} (connected={voice.Client.IsConnected}, inRoom={voice.Client.InRoom})";
        }

        // Recorder 상태 수집
        var recorder = Object.FindFirstObjectByType<Recorder>(FindObjectsInactive.Include);
        string recorderState = "none";
        if (recorder != null)
        {
            recorderState = $"recEnabled={recorder.RecordingEnabled}, isTransmitting={recorder.IsCurrentlyTransmitting}, tx={recorder.TransmitEnabled}";
        }

        // 모든 상태를 한 줄로 출력
        Debug.Log($"[ShutdownDiagnostics] {tag} | photon={photonState} connected={photonConnected} inRoom={photonInRoom} | voice={voiceState} | recorder={recorderState}");
    }

    private void Log(string msg)
    {
        Debug.Log($"[ShutdownDiagnostics] {msg}");
    }
}
#endif
