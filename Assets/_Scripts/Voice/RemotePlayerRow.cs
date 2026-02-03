using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RemotePlayerRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Slider _volSlider;
    [SerializeField] private Toggle _muteToggle;

    private int _actorNumber;
    private bool _suppress;

    public void Setup(VoiceUserSetting user)
    {
        _suppress = true;
        _actorNumber = user.ViewID;
        _nameText.text = user.Nickname;
        _volSlider.value = user.Volume;
        _muteToggle.isOn = user.IsMuted;
        _suppress = false;

        _volSlider.onValueChanged.AddListener(OnVolumeChanged);
        _muteToggle.onValueChanged.AddListener(OnMuteChanged);
    }

    void OnDisable()
    {
        _volSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        _muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
    }

    private void OnVolumeChanged(float v)
    {
        if (_suppress) return;
        VoiceManager.Instance.SetRemoteVolume(_actorNumber, v);
    }

    private void OnMuteChanged(bool isOn)
    {
        if (_suppress) return;

        VoiceManager.Instance.SetRemoteMute(_actorNumber, isOn);

        var view = GetComponentInParent<VoiceOptionsView>();
        if (view != null)
        {
            view.NotifyIndividualMuteChanged();
        }
    }
}