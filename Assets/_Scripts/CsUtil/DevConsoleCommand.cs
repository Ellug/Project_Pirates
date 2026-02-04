public class DevConsoleCommand
{
    private readonly DevConsoleManager _mgr;

    public DevConsoleCommand(DevConsoleManager mgr)
    {
        _mgr = mgr;
    }

    public void Execute(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        string trimmed = raw.Trim();
        string[] parts = trimmed.Split(' ', 2); // 최대 2개로 분리 (명령어, 인자)
        string cmd = parts[0].ToLowerInvariant();
        string arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd)
        {
            case "clear":
                _mgr.ClearLogs();
                _mgr.WriteSystem("Dev Log All Cleared.");
                break;

            case "debug":
                HandleDebug();
                break;

            case "votetime":
                HandleVoteTime();
                break;

            case "destroy":
                HandleDestroy(arg);
                break;

            default:
                _mgr.WriteSystem($"Unknown command: {cmd}");
                break;
        }
    }
    
    // Toggle Debug Mode
    private void HandleDebug()
    {
        _mgr.SetDebugMode(!_mgr.DebugMode);

        string status = _mgr.DebugMode ? "ON" : "OFF";
        _mgr.WriteSystem($"[ DebugMode ] = {status}");
    }

    // 투표 시간을 5초로 설정 (모든 클라이언트 동기화)
    private void HandleVoteTime()
    {
        if (VoteManager.Instance == null)
        {
            _mgr.WriteSystem("[VoteTime] VoteManager가 존재하지 않습니다. 인게임에서만 사용 가능.");
            return;
        }

        // 토론 5초, 투표 5초, 결과 3초로 설정
        VoteManager.Instance.RequestSetVoteTime(5f, 5f, 3f);
        _mgr.WriteSystem("[VoteTime] 투표 시간 변경: 토론 5s, 투표 5s, 결과 3s (모든 클라이언트 동기화)");
    }

    // 씬에서 이름으로 오브젝트 찾아서 파괴
    private void HandleDestroy(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            _mgr.WriteSystem("[Destroy] 사용법: destroy <오브젝트이름>");
            return;
        }

        var obj = UnityEngine.GameObject.Find(objectName);
        if (obj == null)
        {
            _mgr.WriteSystem($"[Destroy] '{objectName}' 오브젝트를 찾을 수 없습니다.");
            return;
        }

        UnityEngine.Object.Destroy(obj);
        _mgr.WriteSystem($"[Destroy] '{objectName}' 오브젝트가 파괴되었습니다.");
    }
}
