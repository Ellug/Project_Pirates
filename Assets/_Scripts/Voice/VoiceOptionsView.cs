using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoiceOptionsView : MonoBehaviour
{
    [Header("Mic Input (Mine)")]
    [SerializeField] private Slider _myMicSlider;
    [SerializeField] private TMP_Dropdown _micTypeDropdown;
    [SerializeField] private Toggle _myMicMuteToggle;

    [Header("Mic Output (Others Master)")]
    [SerializeField] private Slider _masterOutputSlider;
    [SerializeField] private Toggle _masterOutputMuteToggle;

    [Header("Remote Players")]
    [SerializeField] private RectTransform _verticalPanel;
    [SerializeField] private GameObject _playerMicRowPrefab;

    private bool _suppress;
    private List<RemotePlayerRow> _activeRows = new();

    void OnEnable()
    {
        SyncSettingsFromSaved();
        Bind();
        RefreshPlayerList();
    }

    void OnDisable()
    {
        Unbind();
    }

    private void SyncSettingsFromSaved()
    {
        _suppress = true;

        _myMicSlider.value = PlayerPrefs.GetFloat(VoiceParam.MyMicVolumeKey, 1f);
        _micTypeDropdown.value = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);
        _myMicMuteToggle.isOn = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        _masterOutputSlider.value = PlayerPrefs.GetFloat(VoiceParam.MasterOutputKey, 1f);
        _masterOutputMuteToggle.isOn = PlayerPrefs.GetInt(VoiceParam.MasterOutputMuteKey, 0) == 1;

        VoiceManager.Instance?.LoadAndApplySettings();

        _suppress = false;
    }

    private void Bind()
    {
        _myMicSlider.onValueChanged.AddListener(OnMyMicSliderChanged);
        _micTypeDropdown.onValueChanged.AddListener(OnMyMicTypeChanged);
        _myMicMuteToggle.onValueChanged.AddListener(OnMyMicMuteChanged);

        _masterOutputSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        _masterOutputMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);
    }

    private void Unbind()
    {
        _myMicSlider.onValueChanged.RemoveListener(OnMyMicSliderChanged);
        _micTypeDropdown.onValueChanged.RemoveListener(OnMyMicTypeChanged);
        _myMicMuteToggle.onValueChanged.RemoveListener(OnMyMicMuteChanged);

        _masterOutputSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        _masterOutputMuteToggle.onValueChanged.RemoveListener(OnMasterMuteChanged);
    }

    private void RefreshPlayerList()
    {
        StartCoroutine(VoiceManager.Instance.Co_GetVoiceUserSettings(users =>
        {
            foreach (var row in _activeRows) if (row != null) Destroy(row.gameObject);
            _activeRows.Clear();

            foreach (var user in users)
            {
                var go = Instantiate(_playerMicRowPrefab, _verticalPanel);
                var row = go.GetComponent<RemotePlayerRow>();
                if (row != null)
                {
                    row.Setup(user);
                    _activeRows.Add(row);
                }
            }
        }));
    }

    //내 인풋 설정 처리
    private void OnMyMicSliderChanged(float value)
    {
        if (_suppress) return;
        PlayerPrefs.SetFloat(VoiceParam.MyMicVolumeKey, value);
        ApplyMyMicTotal();
    }

    private void OnMyMicTypeChanged(int index)
    {
        if (_suppress) return;
        PlayerPrefs.SetInt(VoiceParam.MyMicTypeKey, index);
        ApplyMyMicTotal();
    }

    private void OnMyMicMuteChanged(bool isOn)
    {
        if (_suppress) return;
        PlayerPrefs.SetInt(VoiceParam.MyMicMuteKey, isOn ? 1 : 0);
        ApplyMyMicTotal();
    }

    private void ApplyMyMicTotal()
    {
        VoiceManager.Instance.ApplyMyMicSettings(_myMicSlider.value, _micTypeDropdown.value, _myMicMuteToggle.isOn);
    }

    //마스터 아웃풋 설정 처리
    private void OnMasterSliderChanged(float value)
    {
        if (_suppress) return;
        PlayerPrefs.SetFloat(VoiceParam.MasterOutputKey, value);
        VoiceManager.Instance.ApplyMasterOutputSettings();
    }

    // 마스터 뮤트 토글 이벤트
    private void OnMasterMuteChanged(bool isOn)
    {
        if (_suppress) return;

        VoiceManager.Instance.SetAllRemoteMute(isOn);

        RefreshPlayerList();
    }

    // 개별 유저 뮤트를 해제했을 때 마스터 토글도 풀리게 처리
    public void NotifyIndividualMuteChanged()
    {
        _suppress = true;
        _masterOutputMuteToggle.isOn = PlayerPrefs.GetInt(VoiceParam.MasterOutputMuteKey, 0) == 1;
        _suppress = false;

        RefreshPlayerList();
    }
}