using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DevConsoleManager : Singleton<DevConsoleManager>
{
    [Header("UI")]
    [SerializeField] private DevConsoleView _view;

    private const string TimestampFormat = "HH:mm:ss:fff";

    // 보관 상한
    private const int MaxLines = 2000;           // 화면/메모리 안정화
    private const int MaxPendingItems = 1024;    // 콘솔 닫힌 상태 폭주 방지 (고유 항목 수 기준)
    private const int MaxMessageChars = 2000;
    private const int MaxStackChars = 6000;

    // ===== 병목 방지용 프레임 예산 =====
    private const int MaxFlushPerFrame = 128;    // 프레임당 flush 처리 개수 상한
    private const float MaxFlushTimeMs = 2f;     // 프레임당 flush 시간 상한(ms)

    private static string Trunc(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
        return s.Substring(0, max) + " ...(truncated)";
    }

    // ===== pending 집계 키 =====
    private readonly struct LogKey : IEquatable<LogKey>
    {
        public readonly LogType type;
        public readonly string msg;
        public readonly string stack;
        private readonly int _hash;

        public LogKey(LogType type, string msg, string stack)
        {
            this.type = type;
            this.msg = msg ?? string.Empty;
            this.stack = stack ?? string.Empty;

            unchecked
            {
                _hash = (int)type;
                _hash = (_hash * 397) ^ this.msg.GetHashCode();
                _hash = (_hash * 397) ^ this.stack.GetHashCode();
            }
        }

        public bool Equals(LogKey other)
            => type == other.type && msg == other.msg && stack == other.stack;

        public override bool Equals(object obj) => obj is LogKey other && Equals(other);
        public override int GetHashCode() => _hash;
    }

    // Log 한 줄에 대한 구조체 (중복 카운트 포함)
    private readonly struct LogItem
    {
        public readonly System.DateTime firstTime;
        public readonly System.DateTime lastTime;
        public readonly LogType type;
        public readonly string msg;
        public readonly string stack;
        public readonly int count;

        public LogItem(System.DateTime time, LogType type, string msg, string stack)
            : this(time, time, type, msg, stack, 1) { }

        private LogItem(System.DateTime first, System.DateTime last, LogType type, string msg, string stack, int count)
        {
            firstTime = first;
            lastTime = last;
            this.type = type;
            this.msg = msg;
            this.stack = stack;
            this.count = count;
        }

        public bool IsSame(LogType type, string msg, string stack)
            => this.type == type && this.msg == msg && this.stack == stack;

        // ===== count만큼 루프 돌리지 않고 O(1) 병합 =====
        public LogItem AddCount(System.DateTime now, int add)
            => new(firstTime, now, type, msg, stack, count + add);

        public LogItem Increment(System.DateTime now) => AddCount(now, 1);

        public LogKey GetKey() => new LogKey(type, msg, stack);
    }

    private readonly object _lock = new(); // 여러 스레드가 동시에 접근하는 데이터를 보호하기 위한 잠금 객체

    // ===== Pending: List -> Dictionary(집계) + Order(순서 유지) =====
    private Dictionary<LogKey, LogItem> _pendingDict = new(256);
    private List<LogKey> _pendingOrder = new(256);
    private bool _pendingOverflow;

    // Flush 분할 처리용(스왑된 배치)
    private Dictionary<LogKey, LogItem> _flushDict;
    private List<LogKey> _flushOrder;
    private int _flushIndex;
    private bool _flushInProgress;

    private readonly List<LogItem> _lines = new(512); // 실제 화면에 출력될 로그

    private DevConsoleCommand _cmd;

    private readonly float _refreshInterval = 0.1f;
    private bool _isOpen;
    private bool _needsRender;
    private float _nextRefreshTime;

    // 디버그 모드. 전역에서 참조해서 특정 기능 부여 가능.
    public bool DebugMode { get; private set; }
    public bool IsOpen => _isOpen;

    protected override void OnSingletonAwake()
    {
        _cmd = new DevConsoleCommand(this); // 명령 처리기 실행

        _view?.Bind(this);
        _view?.SetVisible(false);

        Application.logMessageReceivedThreaded += OnUnityLogThreaded; // 유니티 로그 수신 콜백 등록
    }

    void OnDestroy()
    {
        if (Instance == this)
            Application.logMessageReceivedThreaded -= OnUnityLogThreaded;
    }

    void Update()
    {
        // 콘솔이 닫혀있으면 UI 작업은 안 하고 로그가 pending에 쌓임
        if (!_isOpen) return;

        _view.EnsureInputFocused();

        // ===== [핵심 추가] flush는 매 프레임 예산 내에서 분할 처리 =====
        if (_flushInProgress)
            ContinueFlushBudgeted();
        else
            StartFlushIfNeeded(); // pending이 있으면 스왑 시작

        // 열린 상태에서만 주기 리프레쉬(렌더는 주기 제한)
        if (Time.unscaledTime < _nextRefreshTime) return;
        _nextRefreshTime = Time.unscaledTime + _refreshInterval;

        if (!_needsRender) return;

        _needsRender = false;
        _view.Render(BuildText());
    }

    public void SetOpen(bool open)
    {
        if (_isOpen == open) return;
        Toggle();
    }

    private void Toggle()
    {
        _view?.Bind(this);

        _isOpen = !_isOpen;

        if (_view != null)
        {
            _view.SetVisible(_isOpen);

            if (_isOpen)
            {
                // ===== 열자마자 전체 Flush/BuildText로 프리징 내지 않기 =====
                // - Flush는 Update에서 예산 분할 처리
                // - UI는 즉시 열고 "로딩"만 먼저 표시
                _view.EnsureInputFocused();
                _view.Render("[DevConsole] Loading logs...");
                _nextRefreshTime = 0f; // 즉시 렌더 사이클 시작
                _needsRender = true;

                // pending이 있으면 바로 스왑 시작(락 짧게)
                StartFlushIfNeeded();
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
            _pendingDict.Clear();
            _pendingOrder.Clear();
            _pendingOverflow = false;
        }

        _flushDict = null;
        _flushOrder = null;
        _flushIndex = 0;
        _flushInProgress = false;

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
        msg = Trunc(msg, MaxMessageChars);
        stack = Trunc(stack, MaxStackChars);

        var now = DateTime.Now;

        if (_lines.Count > 0 && _lines[^1].IsSame(type, msg, stack))
            _lines[^1] = _lines[^1].Increment(now);
        else
            _lines.Add(new LogItem(now, type, msg, stack));

        TrimLines();
        _needsRender = true;
    }

    private void OnUnityLogThreaded(string condition, string stackTrace, LogType type)
    {
        // 저장 전에 길이 제한(재귀/스택 폭주 방지)
        condition = Trunc(condition, MaxMessageChars);
        stackTrace = Trunc(stackTrace, MaxStackChars);

        var now = DateTime.Now;
        var key = new LogKey(type, condition, stackTrace);

        lock (_lock)
        {
            // pending 상한: "고유 항목 수" 기준으로 제한
            if (_pendingDict.Count >= MaxPendingItems && !_pendingDict.ContainsKey(key))
            {
                const string overflowMsg = "[DevConsole] Pending overflow. Logs are being suppressed.";
                var overflowKey = new LogKey(LogType.Warning, overflowMsg, null);

                if (_pendingDict.TryGetValue(overflowKey, out var overflowItem))
                {
                    _pendingDict[overflowKey] = overflowItem.Increment(now);
                }
                else if (!_pendingOverflow)
                {
                    _pendingOverflow = true;
                    _pendingDict[overflowKey] = new LogItem(now, LogType.Warning, overflowMsg, null);
                    _pendingOrder.Add(overflowKey);
                }

                return;
            }

            // ===== Dictionary 기반 집계: 동일 로그는 count만 증가 =====
            if (_pendingDict.TryGetValue(key, out var existing))
            {
                _pendingDict[key] = existing.Increment(now);
            }
            else
            {
                _pendingDict[key] = new LogItem(now, type, condition, stackTrace);
                _pendingOrder.Add(key);
            }
        }
    }

    // ===== pending 스왑 시작(락 짧게) =====
    private void StartFlushIfNeeded()
    {
        lock (_lock)
        {
            if (_flushInProgress) return;
            if (_pendingOrder.Count == 0) return;

            _flushDict = _pendingDict;
            _flushOrder = _pendingOrder;
            _flushIndex = 0;
            _flushInProgress = true;

            _pendingDict = new Dictionary<LogKey, LogItem>(256);
            _pendingOrder = new List<LogKey>(256);
            _pendingOverflow = false;
        }
    }

    // ===== 프레임 예산 내 Flush 진행 =====
    private void ContinueFlushBudgeted()
    {
        if (!_flushInProgress || _flushOrder == null || _flushDict == null)
            return;

        float startMs = Time.realtimeSinceStartup * 1000f;
        int processed = 0;

        while (_flushIndex < _flushOrder.Count)
        {
            if (processed >= MaxFlushPerFrame) break;

            float elapsed = (Time.realtimeSinceStartup * 1000f) - startMs;
            if (elapsed >= MaxFlushTimeMs) break;

            var key = _flushOrder[_flushIndex];
            if (_flushDict.TryGetValue(key, out var it))
            {
                MergePendingItemToLines(it);
            }

            _flushIndex++;
            processed++;
        }

        if (processed > 0)
        {
            TrimLines();
            _needsRender = true;
        }

        // 완료
        if (_flushIndex >= _flushOrder.Count)
        {
            _flushInProgress = false;
            _flushDict = null;
            _flushOrder = null;
            _flushIndex = 0;
        }
    }

    // ===== count 병합을 O(1)로 처리 =====
    private void MergePendingItemToLines(LogItem it)
    {
        if (_lines.Count > 0 && _lines[^1].IsSame(it.type, it.msg, it.stack))
        {
            var last = _lines[^1];
            _lines[^1] = last.AddCount(it.lastTime, it.count);
        }
        else
        {
            _lines.Add(it);
        }
    }

    private void TrimLines()
    {
        int overflow = _lines.Count - MaxLines;
        if (overflow <= 0) return;
        _lines.RemoveRange(0, overflow);
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
        // 시간은 마지막 발생 기준
        sb.Append('[').Append(e.lastTime.ToString(TimestampFormat)).Append("][")
        .Append(e.type).Append("] ").Append(e.msg);

        if (e.count > 1)
            sb.Append(" [ +").Append(e.count - 1).Append(" ]"); // 요청하신 형태

        sb.AppendLine();
    }

    // 종료시 txt 저장
    void OnApplicationQuit()
    {
        SaveLogToFile();
    }
}
