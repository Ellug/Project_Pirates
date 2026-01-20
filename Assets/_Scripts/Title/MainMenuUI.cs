using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject _mainMenuCanvas;

    [Header("Panel")]
    [SerializeField] private GameObject _connectPanel;
    [SerializeField] private GameObject _howToPlayPanel;
    [SerializeField] private GameObject _optionPanel;
    [SerializeField] private GameObject _creditPanel;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _gameName;

    void Start()
    {
        CloseAllPanel();
        _mainMenuCanvas.SetActive(true);
        _gameName.gameObject.SetActive(true);
    }

    void OnEnable()
    {
        // Title 씬에서 ESC 입력 시 이 이벤트를 통해 패널 닫기 수행을 위한 구독
        if (InputManager.Instance != null)
            InputManager.Instance.OnEscapeUI += HandleEscapeUI;
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnEscapeUI -= HandleEscapeUI;
    }

    // InputManager.OnEscapeUI 이벤트 핸들러. Title 씬에서 ESC 입력 시 호출
    private void HandleEscapeUI()
    {
        // 열려있는 패널이 있으면 닫기
        if (IsAnyPanelOpen())
            CloseAllPanel();
    }

    // 메인메뉴 외의 패널이 열려있는지 확인
    private bool IsAnyPanelOpen()
    {
        return (_connectPanel != null && _connectPanel.activeSelf) ||
               (_howToPlayPanel != null && _howToPlayPanel.activeSelf) ||
               (_optionPanel != null && _optionPanel.activeSelf) ||
               (_creditPanel != null && _creditPanel.activeSelf);
    }

    public void OnClickGameStart()
    {
        CloseAllPanel();
        _mainMenuCanvas.SetActive(false);
        _connectPanel.SetActive(true);
    }

    public void OnClickHowToPlay()
    {
        CloseAllPanel();
        _howToPlayPanel.SetActive(true);
    }

    public void OnClickOptions()
    {
        CloseAllPanel();
        _optionPanel.SetActive(true);
    }

    public void OnClickCredit()
    {
        CloseAllPanel();
        _creditPanel.SetActive(true);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickExit()
    {
        CloseAllPanel();
        if (_mainMenuCanvas.activeSelf == false)
            _mainMenuCanvas.SetActive(true);

        _gameName.gameObject.SetActive(true);
    }

    private void CloseAllPanel()
    {
        _mainMenuCanvas.SetActive(true);
        _connectPanel.SetActive(false);
        _howToPlayPanel.SetActive(false);
        _optionPanel.SetActive(false);
        _creditPanel.SetActive(false);
    }
}
