using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum InputMode { Player, DevConsole }

public class InputManager : Singleton<InputManager>
{
    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset _actions;

    [Header("UI Roots")]
    [SerializeField] private GameObject _optionsRoot;

    private const string SCENE_INGAME = "InGame";

    private bool _isInGameScene;

    private InputActionMap _globalMap;
    private InputActionMap _uiMap;

    private InputAction _toggleConsole; // F5
    private InputAction _toggleOptions; // ESC
    private InputAction _submit;
    private InputAction _ptt;
    private InputAction _voiceOverlay;

    private PlayerInput _localPlayer;

    private InputMode _mode = InputMode.Player;
    private bool _isOptionsOpen;    

    private const string MAP_PLAYER = "Player";
    private const string MAP_UI     = "UI";
    private const string MAP_GLOBAL = "Global";

    // UI ESC 이벤트: DevConsole 닫힘 상태에서 ESC 시
    // Title/Lobby/Room 등 UI 씬에서 구독하여 자체 ESC 처리 수행
    public event Action OnEscapeUI;
    public event Action OnSubmitUI;
    public event Action<bool> OnPtt;
    public event Action<bool> OnVoiceOverlay;

    public bool IsOptionsOpen => _isOptionsOpen;

    protected override void OnSingletonAwake()
    {
        _globalMap = _actions.FindActionMap(MAP_GLOBAL, true);
        _uiMap = _actions.FindActionMap(MAP_UI, true);

        _toggleConsole = _globalMap.FindAction("ToggleConsole", true);
        _toggleOptions = _globalMap.FindAction("ToggleOptions", true);

        _submit = _uiMap.FindAction("Submit", true);

        _ptt = _globalMap.FindAction("PTT", true);
        _voiceOverlay = _globalMap.FindAction("VoiceOverlay", true);

        _globalMap.Enable();

        // 등록
        _toggleConsole.performed += OnToggleConsole;
        _toggleOptions.performed += OnToggleOptions;

        _submit.started += OnSubmit;

        _ptt.started  += OnPttDown;
        _ptt.canceled += OnPttUp;

        _voiceOverlay.started  += OnVoiceOverlayDown;
        _voiceOverlay.canceled += OnVoiceOverlayUp;

        SceneManager.sceneLoaded += OnSceneLoaded;

        // 시작 씬 반영
        UpdateSceneFlag(SceneManager.GetActiveScene());
        ApplyState();
    }

    private void OnDestroy()
    {
        if (_toggleConsole != null) _toggleConsole.performed -= OnToggleConsole;
        if (_toggleOptions != null) _toggleOptions.performed -= OnToggleOptions;
        if (_submit != null) _submit.started -= OnSubmit;
        if (_ptt != null)
        {
            _ptt.started  -= OnPttDown;
            _ptt.canceled -= OnPttUp;
        }
        if (_voiceOverlay != null)
        {
            _voiceOverlay.started  -= OnVoiceOverlayDown;
            _voiceOverlay.canceled -= OnVoiceOverlayUp;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 로컬 플레이어의 PlayerInput을 등록 InputManager가 ActionMap 전환(Player/UI)을 제어
    public void RegisterLocalPlayer(PlayerInput playerInput)
    {
        _localPlayer = playerInput;
        ApplyState();
    }

    // 로컬 플레이어 등록 해제 (씬 전환/파괴 시 호출)
    public void UnregisterLocalPlayer(PlayerInput playerInput)
    {
        if (_localPlayer == playerInput)
            _localPlayer = null;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        UpdateSceneFlag(s);

        // 씬 바뀌면 옵션은 닫아두기
        _isOptionsOpen = false;

        // 씬 전환 시 기존 _localPlayer 참조 명시적 정리 : MissingReferenceException 발생 방지
        _localPlayer = null;

        // InGame 씬에서만 PlayerInput 자동 탐색
        if (_isInGameScene)
        {
            var playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
                RegisterLocalPlayer(playerInput);
        }

        ApplyState();
    }

    // 씬 플래그 업데이트 - 씬 판별 로직을 한 곳에 집중
    private void UpdateSceneFlag(Scene s)
    {
        _isInGameScene = (s.name == SCENE_INGAME);
    }

    private void OnToggleConsole(InputAction.CallbackContext _)
    {
        _mode = (_mode == InputMode.DevConsole) ? InputMode.Player : InputMode.DevConsole;

        if (DevConsoleManager.Instance != null)
            DevConsoleManager.Instance.SetOpen(_mode == InputMode.DevConsole);

        ApplyState();
    }

    private void OnToggleOptions(InputAction.CallbackContext _)
    {
        // DevConsole 열림 상태에서는 ESC 완전 무시
        if (_mode == InputMode.DevConsole) return;

        // UI ESC 이벤트
        // 구독자가 있으면(Title/Lobby/Room 등) 해당 씬에서 자체 처리
        // 구독자가 없으면(InGame 등) 기존 옵션 토글 로직 수행
        if (OnEscapeUI != null && OnEscapeUI.GetInvocationList().Length > 0)
        {
            OnEscapeUI.Invoke();
            return;
        }

        // InGame 등에서 옵션 토글
        _isOptionsOpen = !_isOptionsOpen;
        ApplyState();
    }

    // 옵션 패널 제어 Public
    // 외부에서 _optionsRoot.SetActive 직접 호출 대신 이 API 사용 권장
    // 어떤 경로로 옵션이 열리고 닫혀도 상태 동기화 목적
    public void OpenOptions()
    {
        if (_mode == InputMode.DevConsole) return; // 콘솔 열림 시 옵션 조작 금지
        if (_isOptionsOpen) return;

        _isOptionsOpen = true;
        ApplyState();
    }

    public void CloseOptions()
    {
        if (!_isOptionsOpen) return;

        _isOptionsOpen = false;
        ApplyState();
    }

    public void ToggleOptions()
    {
        if (_mode == InputMode.DevConsole) return;

        _isOptionsOpen = !_isOptionsOpen;
        ApplyState();
    }

    private void OnSubmit(InputAction.CallbackContext _)
    {
        OnSubmitUI?.Invoke();
    }

    private void OnPttDown(InputAction.CallbackContext _)
    {
        if (_mode == InputMode.DevConsole) return;
        OnPtt?.Invoke(true);
    }

    private void OnPttUp(InputAction.CallbackContext _)
    {
        if (_mode == InputMode.DevConsole) return;
        OnPtt?.Invoke(false);
    }

    private void OnVoiceOverlayDown(InputAction.CallbackContext _)
    {
        if (_mode == InputMode.DevConsole) return;
        OnVoiceOverlay?.Invoke(true);
    }

    private void OnVoiceOverlayUp(InputAction.CallbackContext _)
    {
        if (_mode == InputMode.DevConsole) return;
        OnVoiceOverlay?.Invoke(false);
    }

    /// 게임 결과창 등에서 플레이어 입력 차단 + 커서 해제가 필요할 때 호출
    /// InGame 씬에서 게임 종료 시 사용
    public void SetUIMode(bool uiMode)
    {
        if (_localPlayer != null)
        {
            try
            {
                string map = uiMode ? MAP_UI : MAP_PLAYER;
                _localPlayer.SwitchCurrentActionMap(map);
            }
            catch (MissingReferenceException)
            {
                _localPlayer = null;
            }
        }

        ApplyCursor(uiMode);
    }    

    private void ApplyState()
    {
        if (_optionsRoot != null)
            _optionsRoot.SetActive(_isOptionsOpen);

        bool uiMode = (_mode == InputMode.DevConsole || _isOptionsOpen);

        // 파괴된 오브젝트 참조 방어
        if (_localPlayer != null)
        {
            try
            {
                // 파괴된 오브젝트 접근 시 MissingReferenceException 발생
                var go = _localPlayer.gameObject;
                if (go == null)
                {
                    _localPlayer = null;
                }
                else
                {
                    string map = uiMode ? MAP_UI : MAP_PLAYER;
                    _localPlayer.SwitchCurrentActionMap(map);
                }
            }
            catch (MissingReferenceException)
            {
                _localPlayer = null;
            }
        }

        // 커서 락은 InGame에서만 처리
        ApplyCursor(uiMode);

        _globalMap.Enable();
    }

    private void ApplyCursor(bool uiMode)
    {
        // Title/Lobby/Room 등: 커서 건드리지 않음
        if (!_isInGameScene) return;

        if (uiMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
