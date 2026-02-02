using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SabotageButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SabotageManager _sabotageManager;

    [Header("Buttons")]
    [SerializeField] private Button _engineButton;
    [SerializeField] private Button _blackoutButton;

    [Header("Cooldown")]
    [SerializeField] private float _cooldownTime = 60f;
    [SerializeField] private TMP_Text _cooldownText;

    private float _cooldownRemaining;
    private bool _isOnCooldown;

    void Update()
    {
        if (!_isOnCooldown) return;

        _cooldownRemaining -= Time.deltaTime;

        if (_cooldownText != null)
            _cooldownText.text = Mathf.CeilToInt(_cooldownRemaining).ToString();

        if (_cooldownRemaining <= 0f)
        {
            _isOnCooldown = false;
            _cooldownRemaining = 0f;

            if (_cooldownText != null)
                _cooldownText.text = "";

            SetButtonsInteractable(true);
        }
    }

    public void SetButtonsActive(bool active)
    {
        if (_engineButton != null)
            _engineButton.gameObject.SetActive(active);

        if (_blackoutButton != null)
            _blackoutButton.gameObject.SetActive(active);

        if (_cooldownText != null)
            _cooldownText.gameObject.SetActive(active);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_engineButton != null)
            _engineButton.interactable = interactable;

        if (_blackoutButton != null)
            _blackoutButton.interactable = interactable;
    }

    // 엔진 버튼 OnClick 연결
    public void TriggerEngine()
    {
        if (_isOnCooldown) return;
        if (_sabotageManager == null) return;

        Debug.Log("[SabotageButton] Engine 트리거");
        _sabotageManager.RequestTriggerSabotage(SabotageId.Engine);
        StartCooldown();
    }

    // 블랙아웃 버튼 OnClick 연결
    public void TriggerBlackout()
    {
        if (_isOnCooldown) return;
        if (_sabotageManager == null) return;

        Debug.Log("[SabotageButton] Blackout 트리거");
        _sabotageManager.RequestTriggerSabotage(SabotageId.Light);
        StartCooldown();
    }

    private void StartCooldown()
    {
        _isOnCooldown = true;
        _cooldownRemaining = _cooldownTime;

        SetButtonsInteractable(false);

        if (_cooldownText != null)
            _cooldownText.text = Mathf.CeilToInt(_cooldownTime).ToString();
    }
}
