using UnityEngine;

public class OptionMenuView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _graphicPanel;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _voicePanel;
    [SerializeField] private GameObject _controllPanel;

    public void CloseOptionPanel()
    {
        _root.SetActive(false);
    }

    public void OnClickTab(GameObject Panel)
    {
        CloseAllTabs();
        Panel.SetActive(true);
    }
    
    private void CloseAllTabs()
    {
        _graphicPanel.SetActive(false);
        _audioPanel.SetActive(false);
        _voicePanel.SetActive(false);
        _controllPanel.SetActive(false);
    }
}
