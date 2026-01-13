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
}
