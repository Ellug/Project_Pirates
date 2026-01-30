using UnityEngine;

public class VoiceUIController : Singleton<VoiceUIController>
{
    [Header("UI Prefab")]
    [SerializeField] private GameObject _voiceOptionsPrefab; // 에셋 프리팹

    private GameObject _instance;
    private bool _cursorState;

    void Start()
    {
        InputManager.Instance.OnVoiceOverlay += HandleVoiceOverlay;
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnVoiceOverlay -= HandleVoiceOverlay;
    }

    private void HandleVoiceOverlay(bool pressed)
    {
        if (GameManager.Instance != null)
        {
            SceneState currentState = GameManager.Instance.FlowState;
            bool allowed = (currentState == SceneState.Room || currentState == SceneState.InGame);

            if (!allowed)
            {
                if (_instance != null && _instance.activeSelf)
                    SetVisible(false);
                return;
            }
        }

        if (pressed)
        {
            _cursorState = (Cursor.lockState != CursorLockMode.Locked);
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_instance == null && _voiceOptionsPrefab != null)
        {
            _instance = Instantiate(_voiceOptionsPrefab);
            _instance.SetActive(false);
        }

        if (_instance != null)
        {
            _instance.SetActive(visible);
        }

        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (!_cursorState)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}