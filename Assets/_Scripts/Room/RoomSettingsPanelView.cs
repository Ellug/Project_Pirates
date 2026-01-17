using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoomSettingsPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("Room Settings")]
    [SerializeField] private TMP_InputField _titleInput;
    [SerializeField] private TMP_InputField _pwInput;
    [SerializeField] private TMP_Dropdown _maxPlayerDropdown;
    [SerializeField] private Button _applyButton;

    public event Action<string, string, int> ApplyRequested;

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

    // 방장만 true
    public void SetInteractable(bool canEdit)
    {
        if (_titleInput != null) _titleInput.interactable = canEdit;
        if (_pwInput != null) _pwInput.interactable = canEdit;
        if (_maxPlayerDropdown != null) _maxPlayerDropdown.interactable = canEdit;
        if (_applyButton != null) _applyButton.interactable = canEdit;
    }

}
