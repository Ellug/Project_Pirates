using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _staminaBar;
    [SerializeField] private Image[] _itemSlots;

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        _model = model;

        _model.OnHealthChanged += UpdateHealth;
        _model.OnStaminaChanged += UpdateStamina;
        _model.OnItemSlotChanged += UpdatePlayerItem;

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
        _model.OnItemSlotChanged -= UpdatePlayerItem;
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

    private void UpdatePlayerItem(ItemData[] curSlots)
    {
        // 아이템 슬롯 확인 후 있다면 스프라이트 넣고 없으면 빈칸.
        for (int i = 0; i < _itemSlots.Length; i++)
        {
            if (curSlots[i] == null)
            {
                _itemSlots[i].sprite = null;
                _itemSlots[i].color = new Color(1, 1, 1, 0);
            }
            else
            {
                _itemSlots[i].sprite = curSlots[i].icon;
                _itemSlots[i].color = new Color(1, 1, 1, 1);
            }
        }
    }
}
