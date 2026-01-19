using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceUIController : Singleton<VoiceUIController>
{
    [Header("UI Prefab")]
    [SerializeField] private GameObject _voiceOptionsPrefab; // 에셋 프리팹

    private GameObject _instance;
    private bool _cursorState;

    void Update()
    {
        if (Keyboard.current == null) return;
        
        if (GameManager.Instance != null)
        {
            SceneState currentState = GameManager.Instance.FlowState;
            if (currentState != SceneState.Room && currentState != SceneState.InGame)
            {
                if (_instance != null && _instance.activeSelf)
                {
                    SetVisible(false);
                }
                return;
            }
        }

        if (Keyboard.current[Key.Tab].wasPressedThisFrame)
        {
            _cursorState = (Cursor.lockState != CursorLockMode.Locked);
            SetVisible(true);
        }

        if (Keyboard.current[Key.Tab].wasReleasedThisFrame)
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