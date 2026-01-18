using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject _mainMenuCanvas;

    [Header("Panel")]
    [SerializeField] private GameObject _connectPanel;
    // [SerializeField] private GameObject _mainMenuListPanel;
    [SerializeField] private GameObject _howToPlayPanel;
    [SerializeField] private GameObject _optionPanel;
    [SerializeField] private GameObject _creditPanel;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _gameName;

    void Start()
    {
        CloseAllPanel();
        _mainMenuCanvas.SetActive(true);
        // _mainMenuListPanel.SetActive(true);
        _gameName.gameObject.SetActive(true);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseAllPanel();
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
        if(_mainMenuCanvas.activeSelf == false)
        {
            _mainMenuCanvas.SetActive(true);
        }
        // _mainMenuListPanel.SetActive(true);
        _gameName.gameObject.SetActive(true);
    }
    private void CloseAllPanel()
    {
        _connectPanel.SetActive(false);
        // _mainMenuListPanel.SetActive(false);
        _howToPlayPanel.SetActive(false);
        _optionPanel.SetActive(false);
        _creditPanel.SetActive(false);
        // _gameName.gameObject.SetActive(false);
    }
}
