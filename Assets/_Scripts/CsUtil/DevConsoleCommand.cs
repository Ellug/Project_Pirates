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

        // 앞뒤 공백만 제거하고 소문자로 변환하여 직접 비교
        string cmd = raw.Trim().ToLowerInvariant();

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
}
