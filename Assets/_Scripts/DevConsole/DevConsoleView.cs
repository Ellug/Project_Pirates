using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DevConsoleView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private TMP_InputField _input;

    private DevConsoleManager _mgr;
    private bool _bound;
    private bool _lockFocus;

    public void Bind(DevConsoleManager mgr)
    {
        if (_bound) return;

        _mgr = mgr;

        if (_input != null)
            _input.onSubmit.AddListener(OnSubmit);

        _bound = true;
    }

    void LateUpdate()
    {
        if (_lockFocus) ForceFocus();
    }

    void OnDestroy()
    {
        if (_input != null)
            _input.onSubmit.RemoveListener(OnSubmit);

        _bound = false;
    }

    private void ForceFocus()
    {
        if (_input == null) return;

        // 선택이 다른 곳으로 튀면 되돌림
        var es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject != _input.gameObject)
            es.SetSelectedGameObject(_input.gameObject);

        if (!_input.isFocused)
        {
            _input.ActivateInputField();
            _input.Select();
        }

        _input.caretPosition = _input.text.Length;
        _input.ForceLabelUpdate();
    }

    private void OnSubmit(string value)
    {
        if (_mgr == null) return;

        _mgr.SubmitCommand(value);

        if (_input != null)
        {
            _input.text = string.Empty;
            EnsureInputFocused();
        }
    }

    public void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
        else gameObject.SetActive(visible);

        _lockFocus = visible;

        if (visible)
            EnsureInputFocused();
    }

    public void Render(string text)
    {
        if (_text == null) return;

        _text.text = text;

        Canvas.ForceUpdateCanvases(); // ScrollToBatoom 보장을 위해
        ScrollToBottom();
    }

    public void ScrollToBottom()
    {
        if (_scroll == null) return;
        _scroll.verticalNormalizedPosition = 0f;
    }

    public void EnsureInputFocused()
    {
        if (_input == null) return;

        if (!_input.isFocused)
        {
            _input.ActivateInputField();
            _input.Select();

            // Caret 표시
            _input.caretPosition = _input.text.Length;
            _input.ForceLabelUpdate();
        }
    }
}
