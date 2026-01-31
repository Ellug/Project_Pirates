using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GachaMission : MissionBase
{
    [Header("UI - Buttons")]
    [SerializeField] private Button _gainCoinButton;
    [SerializeField] private Button _roll10Button;   

    [Header("UI - Text")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _infoText;
    [SerializeField] private TMP_Text _resultText;

    [Header("UI - Result Slots (size 10)")]
    [SerializeField] private Image[] _slots;
    [SerializeField] private Sprite _sprite3;
    [SerializeField] private Sprite _sprite4;
    [SerializeField] private Sprite _sprite5;

    [Header("Config")]
    [SerializeField] private int _startCoin = 0;
    [SerializeField] private int _coinPerClick = 20;
    [SerializeField] private int _cost10Pull = 1000;

    [Header("Refund")]
    [SerializeField] private int _refundPer3 = 10;
    [SerializeField] private int _refundPer4 = 40;

    [Header("Rates")]
    [Range(0f, 1f)][SerializeField] private float _rate5 = 0.03f;
    [Range(0f, 1f)][SerializeField] private float _rate4 = 0.37f;

    [Header("Pity")]
    [SerializeField] private int _pityCheckPull = 90;
    [SerializeField] private int _pityConfirmPull = 100; // 100연차 때 5성 확정

    private int _coin;
    private int _totalPulls;     // 누적 뽑기 횟수(1연차 = 1)
    private bool _gotAny5Star;   // 지금까지 5성 뽑은 적 있는지
    private bool _ended;

    public override void Init()
    {
        _coin = _startCoin;
        _totalPulls = 0;
        _gotAny5Star = false;
        _ended = false;

        // 슬롯 초기화
        if (_slots != null)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].sprite = null;
                    _slots[i].color = new Color(1, 1, 1, 0);
                }
            }
        }

        if (_gainCoinButton != null)
        {
            _gainCoinButton.onClick.RemoveAllListeners();
            _gainCoinButton.onClick.AddListener(GainCoin);
        }

        if (_roll10Button != null)
        {
            _roll10Button.onClick.RemoveAllListeners();
            _roll10Button.onClick.AddListener(Roll10);
        }

        RefreshUI();
        SetResult("5성을 뽑으면 성공!");
    }

    private void GainCoin()
    {
        if (_ended) return;

        _coin += _coinPerClick;
        RefreshUI();
        SetResult($"+{_coinPerClick} 코인 획득!");
    }

    private void Roll10()
    {
        if (_ended) return;

        if (_coin < _cost10Pull)
        {
            SetResult($"코인이 부족합니다! (필요: {_cost10Pull}, 보유: {_coin})");
            return;
        }

        _coin -= _cost10Pull;

        int count3 = 0;
        int count4 = 0;

        bool got5InThis10 = false;

        for (int i = 0; i < 10; i++)
        {
            int rarity = RollRarityWithPity(); // 3/4/5
            ApplySlot(i, rarity);

            if (rarity == 5)
            {
                got5InThis10 = true;
                _gotAny5Star = true;
            }
            else if (rarity == 4) count4++;
            else count3++;
        }

        // 3~4성 환급
        int refund = (count3 * _refundPer3) + (count4 * _refundPer4);
        if (!got5InThis10 && refund > 0)
        {
            _coin += refund;
        }

        RefreshUI();

        // 결과 텍스트
        string refundMsg = (!got5InThis10 && refund > 0) ? $" / 환급 +{refund}" : "";
        SetResult($"10연차 결과: 5성 {(got5InThis10 ? "획득!" : "없음")} / 4성 {count4} / 3성 {count3}{refundMsg}");

        // 성공 처리
        if (got5InThis10)
        {
            SuccessAndClose();
        }
    }

    private int RollRarityWithPity()
    {
        // 이번 뽑기가 몇 번째인지 먼저 증가
        _totalPulls++;

        // 90연차 동안 5성이 한번도 뜨지 않았을 경우 100연차 확정
        bool pityActive = (!_gotAny5Star && _totalPulls > _pityCheckPull);
        bool isGuaranteePull = (!_gotAny5Star && _totalPulls == _pityConfirmPull);

        if (isGuaranteePull)
            return 5;

        // 일반 확률
        float r = Random.value;
        if (r < _rate5) return 5;
        if (r < _rate5 + _rate4) return 4;
        return 3;
    }

    private void ApplySlot(int index, int rarity)
    {
        if (_slots == null || index < 0 || index >= _slots.Length) return;
        if (_slots[index] == null) return;

        _slots[index].color = Color.white;

        switch (rarity)
        {
            case 5: _slots[index].sprite = _sprite5; break;
            case 4: _slots[index].sprite = _sprite4; break;
            default: _slots[index].sprite = _sprite3; break;
        }
    }

    private void SuccessAndClose()
    {
        if (_ended) return;
        _ended = true;

        // 입력 막기
        if (_gainCoinButton != null) _gainCoinButton.interactable = false;
        if (_roll10Button != null) _roll10Button.interactable = false;

        SetResult("5성 획득! 미션 성공! 잠시 후 종료됩니다.");
        StartCoroutine(Co_CloseAfterSeconds(2f));
    }

    private IEnumerator Co_CloseAfterSeconds(float sec)
    {
        yield return new WaitForSeconds(sec);
        CompleteMission();
    }

    private void RefreshUI()
    {
        if (_coinText != null)
            _coinText.text = $"Coin : {_coin}";

        // 천장 상태 표시용
        if (_infoText != null)
        {
            string pityState = (!_gotAny5Star && _totalPulls >= _pityCheckPull)
                ? $"천장 진행중: {_totalPulls}/{_pityConfirmPull}"
                : $"누적 연차: {_totalPulls}/{_pityConfirmPull}";

            _infoText.text =
                $"5성: {_rate5 * 100f:0.#}% / 4성: {_rate4 * 100f:0.#}% / 3성: {(1f - (_rate5 + _rate4)) * 100f:0.#}%\n" +
                $"10연차 비용: {_cost10Pull}코인 / 클릭당 +{_coinPerClick}코인\n" +
                $"환급: 3성 1개당 +{_refundPer3}, 4성 1개당 +{_refundPer4} (단, 10연차에 5성 없을 때만)\n" +
                $"천장: 90연차 동안 5성 0회면 100연차 5성 확정\n" +
                $"{pityState}";
        }
    }

    private void SetResult(string msg)
    {
        if (_resultText != null)
            _resultText.text = msg;
    }
}
