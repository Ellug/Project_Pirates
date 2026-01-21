using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerSettingView : MonoBehaviour
{
    [SerializeField] Slider _sensitivitySlider;
    [SerializeField] TMP_InputField _sensitivityInputField;

    private void Start()
    {
        _sensitivitySlider.minValue = 0.1f;
        _sensitivitySlider.maxValue = 15.0f;

        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 15.0f);
        
        _sensitivitySlider.value = savedSens;
        _sensitivityInputField.text = savedSens.ToString("F1");

        _sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        _sensitivityInputField.onEndEdit.AddListener(OnInputFieldValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        _sensitivityInputField.text = value.ToString("F1");

        ApplySensitivity(value);
    }

    private void OnInputFieldValueChanged(string text)
    {
        if (float.TryParse(text, out float newValue))
        {
            newValue = Mathf.Clamp(newValue, _sensitivitySlider.minValue, _sensitivitySlider.maxValue);

            _sensitivitySlider.value = newValue;
            _sensitivityInputField.text = newValue.ToString("F1");
            ApplySensitivity(newValue);
        }
        else
        {
            _sensitivityInputField.text = _sensitivitySlider.value.ToString("F1");
        }
    }

    private void ApplySensitivity(float value)
    {
        if (PlayerController.LocalInstancePlayer != null)
        {
            var controller = PlayerController.LocalInstancePlayer.GetComponent<PlayerController>();
            controller?.UpdateSensitivity(value);
        }
    }
}
