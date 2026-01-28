using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapPanel : MonoBehaviour
{
    [SerializeField] private GameObject _mapPanel;

    [Header("UI")]
    [SerializeField] private TMP_Text _mapPageText;
    [SerializeField] private Button _pageUpButton;
    [SerializeField] private Button _pageDownButton;
    [SerializeField] private GameObject[] _mapPages;

    [Header("Input")]
    [SerializeField] private InputActionAsset _actions;

    private InputActionMap _playerMap;
    private InputAction _mapPageUp;
    private InputAction _mapPageDown;
    private InputAction _toggleMap;

    private int _currentPageIndex;
    private bool _isOpen;

    void Awake()
    {
        _playerMap = _actions.FindActionMap("Player", true);
        _mapPageUp = _playerMap.FindAction("MapPageUp", true);
        _mapPageDown = _playerMap.FindAction("MapPageDown", true);
        _toggleMap = _playerMap.FindAction("ToggleMap", true);

        _toggleMap.performed += OnToggleMap;
    }

    void OnDestroy()
    {
        if (_toggleMap != null)
            _toggleMap.performed -= OnToggleMap;
    }

    private void OnToggleMap(InputAction.CallbackContext _)
    {
        _isOpen = !_isOpen;
        _mapPanel.SetActive(_isOpen);
        ApplyCursor(_isOpen);
    }

    private void ApplyCursor(bool unlock)
    {
        if (unlock)
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

    void OnEnable()
    {
        _mapPageUp.performed += OnMapPageUp;
        _mapPageDown.performed += OnMapPageDown;

        _pageUpButton.onClick.AddListener(PageUp);
        _pageDownButton.onClick.AddListener(PageDown);

        // 초기 페이지 설정
        SetPage(_currentPageIndex);
    }

    void OnDisable()
    {
        _mapPageUp.performed -= OnMapPageUp;
        _mapPageDown.performed -= OnMapPageDown;

        _pageUpButton.onClick.RemoveListener(PageUp);
        _pageDownButton.onClick.RemoveListener(PageDown);
    }

    private void OnMapPageUp(InputAction.CallbackContext _) => PageUp();
    private void OnMapPageDown(InputAction.CallbackContext _) => PageDown();

    private void PageUp()
    {
        if (_currentPageIndex >= _mapPages.Length - 1) return;
        SetPage(_currentPageIndex + 1);
    }

    private void PageDown()
    {
        if (_currentPageIndex <= 0) return;
        SetPage(_currentPageIndex - 1);
    }

    private void SetPage(int index)
    {
        _currentPageIndex = index;

        // 모든 페이지 비활성화 후 현재 페이지만 활성화
        for (int i = 0; i < _mapPages.Length; i++)
            _mapPages[i].SetActive(i == _currentPageIndex);

        // 텍스트 업데이트
        _mapPageText.text = $"{_currentPageIndex + 1} Floor Map";

        // 버튼 활성화 상태 업데이트
        _pageDownButton.interactable = _currentPageIndex > 0;
        _pageUpButton.interactable = _currentPageIndex < _mapPages.Length - 1;
    }
}
