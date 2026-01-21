using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _staminaBar;

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        _model = model;

        _model.OnHealthChanged += UpdateHealth;
        _model.OnStaminaChanged += UpdateStamina;

        // 초기값 세팅
        UpdateHealth(model.CurHP, model.MaxHP);
        UpdateStamina(model.CurStamina, model.MaxStamina);
    }

    private void OnDestroy()
    {
        UnBind();
    }

    private void UnBind()
    {
        if (_model == null) return;

        _model.OnHealthChanged -= UpdateHealth;
        _model.OnStaminaChanged -= UpdateStamina;
        _model = null;
    }

    private void UpdateHealth(float cur, float max)
    {
        _hpBar.value = cur / max;
    }

    private void UpdateStamina(float cur, float max)
    {
        _staminaBar.value = cur / max;
    }
}
