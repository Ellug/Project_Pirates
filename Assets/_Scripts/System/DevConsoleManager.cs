using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class DevConsoleManager : Singleton<DevConsoleManager>
{
    [Header("UI")]
    [SerializeField] private DevConsoleView _viewPrefab;

    private const string TimestampFormat = "HH:mm:ss:fff";

    // Log 한 줄에 대한 구조체
    private readonly struct LogItem
    {
        public readonly System.DateTime time;
        public readonly LogType type; // 유니티가 정의한 Enum : Log, Warning, Error, Exception
        public readonly string msg;
        public readonly string stack; // 스택 트레이스

        public LogItem(System.DateTime time, LogType type, string msg, string stack)
        {
            this.time = time;
            this.type = type;
            this.msg = msg;
            this.stack = stack;
        }
    }
    
    private readonly object _lock = new(); // 여러 스레드가 동시에 접근하는 데이터를 보호하기 위한 잠금 객체
    private readonly Queue<LogItem> _pending = new(256); // 로그 임시 저장 큐 (lock으로 보호)
    private readonly List<LogItem> _lines = new(512); // 실제 화면에 출력될 로그

    private DevConsoleView _view;
    private DevConsoleCommand _cmd;
    
    private readonly float _refreshInterval = 0.1f;
    private bool _isOpen;
    private bool _needsRender;
    private float _nextRefreshTime;

    // 디버그 모드. 전역에서 참조해서 특정 기능 부여 가능.
    public bool DebugMode { get; private set; }

    protected override void OnSingletonAwake()
    {
        _cmd = new DevConsoleCommand(this); // 명령 처리기 실행

        EnsureViewInstance(); // UI 확보
        _view?.Bind(this);
        _view?.SetVisible(false);

        Application.logMessageReceivedThreaded += OnUnityLogThreaded; // 유니티 로그 수신 콜백 등록
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Application.logMessageReceivedThreaded -= OnUnityLogThreaded;
    }

    private void Update()
    {
        // TODO : Input System 변경 , Toggle 시 Input 맵 변경 등을 통해 다른 인풋 방지
        if (Keyboard.current.f5Key.wasPressedThisFrame)
            Toggle();

        // 콘솔이 닫혀있으면 UI 작업은 안 하고 로그가 큐에 쌓임
        if (!_isOpen) return;

        _view.EnsureInputFocused();

        // 열린 상태에서만 주기 리프레쉬
        if (Time.unscaledTime < _nextRefreshTime) return;
        _nextRefreshTime = Time.unscaledTime + _refreshInterval;

        FlushPendingToLines();
        if (!_needsRender) return;

        _needsRender = false;
        _view.Render(BuildText());
    }

    public void Toggle()
    {
        EnsureViewInstance();
        _view?.Bind(this);

        _isOpen = !_isOpen;

        if (_view != null)
        {
            _view.SetVisible(_isOpen);

            if (_isOpen)
            {
                // 열면 누적된 로그 반영
                FlushPendingToLines();
                _needsRender = true;
                _view.EnsureInputFocused();
                _view.Render(BuildText());
                _view.ScrollToBottom();
            }
        }
    }

    public void SubmitCommand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        WriteSystem($"> {raw}");
        _cmd.Execute(raw);
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            _pending.Clear();
        }

        _lines.Clear();
        _needsRender = true;

        if (_isOpen)
            _view?.Render(string.Empty);
    }

    public void SetDebugMode(bool on)
    {
        DebugMode = on;
    }

    public void WriteSystem(string msg)
    {
        AddLocal(LogType.Log, msg, null);
    }

    private void AddLocal(LogType type, string msg, string stack)
    {        
        _lines.Add(new LogItem(System.DateTime.Now, type, msg, stack));

        _needsRender = true;
    }

    private void OnUnityLogThreaded(string condition, string stackTrace, LogType type)
    {
        lock (_lock)
        {
            _pending.Enqueue(new LogItem(System.DateTime.Now, type, condition, stackTrace));
        }
    }

    private void FlushPendingToLines()
    {
        bool hasNew = false;

        lock (_lock)
        {
            while (_pending.Count > 0)
            {
                _lines.Add(_pending.Dequeue()); // 큐에서 line으로
                hasNew = true;
            }
        }

        if (hasNew)
            _needsRender = true;
    }

    private string BuildText()
    {
        var sb = new StringBuilder(4096);

        for (int i = 0; i < _lines.Count; i++)
        {
            var e = _lines[i];
            AppendLogLine(sb, e);

            if ((e.type == LogType.Error || e.type == LogType.Exception) && !string.IsNullOrEmpty(e.stack))
                sb.AppendLine(e.stack);
        }

        return sb.ToString();
    }

    private void EnsureViewInstance()
    {
        if (_view != null) return;

        // 1) 씬에 이미 존재하면 사용
        var existing = Object.FindAnyObjectByType<DevConsoleView>(FindObjectsInactive.Include);
        if (existing != null && existing.gameObject.scene.IsValid())
        {
            _view = existing;
            _view.transform.SetParent(transform, false); // 매니저 아래로 붙여서 함께 유지
            return;
        }

        // 2) 없으면 프리팹으로 생성
        if (_viewPrefab == null)
        {
            Debug.LogWarning("[DevConsole] DevConsoleView prefab not assigned.");
            return;
        }

        _view = Instantiate(_viewPrefab, transform, false);
    }

    // txt로 저장
    private void SaveLogToFile()
    {
        try
        {
            if (_lines.Count == 0) return;

            var sb = new StringBuilder(8192);

            for (int i = 0; i < _lines.Count; i++)
            {
                var e = _lines[i];
                AppendLogLine(sb, e);

                if ((e.type == LogType.Error || e.type == LogType.Exception) && !string.IsNullOrEmpty(e.stack))
                    sb.AppendLine(e.stack);
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string dirPath = System.IO.Path.Combine(Application.persistentDataPath, "DevLog");

            System.IO.Directory.CreateDirectory(dirPath);

            string fileName = $"DevConsoleLog_{timestamp}.txt";
            string filePath = System.IO.Path.Combine(dirPath, fileName);

            System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[DevConsole] Log saved: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DevConsole] Failed to save log file\n{ex}");
        }
    }

    private static void AppendLogLine(StringBuilder sb, LogItem e)
    {
        sb.Append('[').Append(e.time.ToString(TimestampFormat)).Append("][")
            .Append(e.type).Append("] ").AppendLine(e.msg);
    }

    // 종료시 txt 저장
    void OnApplicationQuit()
    {
        SaveLogToFile();
    }
}
