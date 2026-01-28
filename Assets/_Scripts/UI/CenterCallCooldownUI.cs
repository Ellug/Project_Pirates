using UnityEngine;
using TMPro;

public class CenterCallCooldownUI : MonoBehaviour
{
    [SerializeField] private CenterCall _centerCall;
    [SerializeField] private GameObject _cooldownContainer;
    [SerializeField] private TextMeshProUGUI _cooldownText;

    void Update()
    {
        if (_centerCall == null) return;

        bool isOnCooldown = _centerCall.IsOnCooldown;

        if (_cooldownContainer != null)
            _cooldownContainer.SetActive(isOnCooldown);

        if (_cooldownText != null)
        {
            if (isOnCooldown)
            {
                float remaining = _centerCall.RemainingCooldown;
                _cooldownText.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                _cooldownText.text = "";
            }
        }
    }
}
