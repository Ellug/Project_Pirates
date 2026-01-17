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

        if (_applyButton != null)
            _applyButton.onClick.AddListener(ApplyClicked);
    }

    void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Close);

        if (_applyButton != null)
            _applyButton.onClick.RemoveListener(ApplyClicked);
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

    private void ApplyClicked()
    {
        string title = _titleInput != null ? _titleInput.text.Trim() : "";
        string pw = _pwInput != null ? _pwInput.text.Trim() : "";

        int max = 0;
        if(_maxPlayerDropdown != null && _maxPlayerDropdown.options.Count > 0)
        {
            string raw = _maxPlayerDropdown.options[_maxPlayerDropdown.value].text;
            raw = raw.Replace("명", "").Trim();
            int.TryParse(raw, out max);
        }
        ApplyRequested?.Invoke(title, pw, max);
        Close();
    }

    public void SetFields(string title, string pw, int maxPlayers)
    {
        if (_titleInput != null) _titleInput.text = title ?? "";
        if (_pwInput != null) _pwInput.text = pw ?? "";

        if(_maxPlayerDropdown != null && _maxPlayerDropdown.options.Count > 0)
        {
            int index = 0;
            for(int i = 0; i < _maxPlayerDropdown.options.Count; i++)
            {
                string raw = _maxPlayerDropdown.options[i].text.Replace("명", "").Trim();
                if(int.TryParse(raw, out int v) && v == maxPlayers)
                {
                    index = i;
                    break;
                }
                _maxPlayerDropdown.value = index;
                _maxPlayerDropdown.RefreshShownValue();
            }
        }
    }
}
