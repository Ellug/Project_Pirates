using UnityEngine;
using UnityEngine.UI;

public sealed class AudioOptionsView : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _uiSlider;

    [SerializeField] private Slider _micInputSlider;
    [SerializeField] private Slider _micOutputSlider;

    private const float DEFAULT_MASTER = 1f;
    private const float DEFAULT_BGM    = 1f;
    private const float DEFAULT_UI     = 1f;

    private const float DEFAULT_MICINPUT = 1f;
    private const float DEFAULT_MICOUTPUT = 1f;

    private bool _suppress;

    private void OnEnable()
    {
        SyncSlidersFromSaved();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
        PlayerPrefs.Save(); // 패널 닫을 때 1회만 저장
    }

    private void SyncSlidersFromSaved()
    {
        _suppress = true;

        float master    = PlayerPrefs.GetFloat(AudioParam.MASTER_KEY, DEFAULT_MASTER);
        float bgm       = PlayerPrefs.GetFloat(AudioParam.BGM_KEY, DEFAULT_BGM);
        float ui        = PlayerPrefs.GetFloat(AudioParam.UI_KEY, DEFAULT_UI);
        
        float micInput  = PlayerPrefs.GetFloat(VoiceParam.MasterInputKey, DEFAULT_MICINPUT);
        float micOutput = PlayerPrefs.GetFloat(VoiceParam.MyMicVolumeKey, DEFAULT_MICOUTPUT);

        if (_masterSlider != null)    _masterSlider.SetValueWithoutNotify(master);
        if (_bgmSlider != null)       _bgmSlider.SetValueWithoutNotify(bgm);
        if (_uiSlider != null)        _uiSlider.SetValueWithoutNotify(ui);

        if (_micInputSlider != null)  _micInputSlider.SetValueWithoutNotify(micInput);
        if (_micOutputSlider != null) _micOutputSlider.SetValueWithoutNotify(micOutput);

        if (AudioManager.Instance != null)
            AudioManager.Instance.LoadAndApplySavedVolumes();

        if (VoiceManager.Instance != null)
            VoiceManager.Instance.LoadAndApplySettings();

        _suppress = false;
    }

    private void Bind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_uiSlider != null)        _uiSlider.onValueChanged.AddListener(OnUiChanged);
        
        if (_micInputSlider != null)  _micInputSlider.onValueChanged.AddListener(OnMicInputChanged);
        if (_micOutputSlider != null) _micOutputSlider.onValueChanged.AddListener(OnMicOutputChanged);
    }

    private void Unbind()
    {
        if (_masterSlider != null)    _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_bgmSlider != null)       _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (_uiSlider != null)        _uiSlider.onValueChanged.RemoveListener(OnUiChanged);

        if (_micInputSlider != null)  _micInputSlider.onValueChanged.RemoveListener(OnMicInputChanged);
        if (_micOutputSlider != null) _micOutputSlider.onValueChanged.RemoveListener(OnMicOutputChanged);
    }

    private void OnMasterChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.Master, v);
        PlayerPrefs.SetFloat(AudioParam.MASTER_KEY, v);
    }

    private void OnBgmChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.BGM, v);
        PlayerPrefs.SetFloat(AudioParam.BGM_KEY, v);
    }

    private void OnUiChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        AudioManager.Instance.SetVolume(AudioBus.UI, v);
        PlayerPrefs.SetFloat(AudioParam.UI_KEY, v);
    }
    private void OnMicInputChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(VoiceParam.MasterInputKey, v);
        VoiceManager.Instance.ApplyMasterInputSettings();
    }

    private void OnMicOutputChanged(float v)
    {
        if (_suppress) return;

        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(VoiceParam.MyMicVolumeKey, v);

        int type = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);
        bool isMuted = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        VoiceManager.Instance.ApplyMyMicSettings(v, type, isMuted);
    }

    // 초기화 버튼용
    public void ResetToDefault()
    {
        PlayerPrefs.SetFloat(AudioParam.MASTER_KEY, DEFAULT_MASTER);
        PlayerPrefs.SetFloat(AudioParam.BGM_KEY, DEFAULT_BGM);
        PlayerPrefs.SetFloat(AudioParam.UI_KEY, DEFAULT_UI);

        PlayerPrefs.SetFloat(VoiceParam.MasterInputKey, DEFAULT_MICINPUT);
        PlayerPrefs.SetFloat(VoiceParam.MyMicVolumeKey, DEFAULT_MICOUTPUT);
        PlayerPrefs.Save();

        SyncSlidersFromSaved();
    }
}
