using UnityEngine;
using UnityEngine.UI;

public sealed class RoomSettingsPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (_root != null) _root.SetActive(true);
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
    }
}
