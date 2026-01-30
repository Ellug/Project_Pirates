using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpAndDownMission : MissionBase
{
    [Header("UI")]
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _chanceText;
    [SerializeField] private Button _submitButton;

    [Header("Rule")]
    [SerializeField] private int _min = 1;
    [SerializeField] private int _max = 100;
    [SerializeField] private int _maxTry = 7;

    [Header("Auto Quit Delay")]
    [SerializeField] private float _exitDelay = 2f;

    private int _answer;
    private int _remain;

    private bool _isEnding;
    private Coroutine _exitCor;

    public override void Init()
    {
        _answer = Random.Range(_min, _max + 1);
        _remain = _maxTry;
        _isEnding = false;

        if(_exitCor != null)
        {
            StopCoroutine(_exitCor);
            _exitCor = null;
        }

        if (_resultText != null)
            _resultText.text = $"{_min}~{_max} 숫자를 맞춰보세요";

        UpdateChanceText();

        if(_input != null)
        {
            // 숫자만 입력 가능
            _input.contentType = TMP_InputField.ContentType.IntegerNumber;
            _input.text = "";
            _input.interactable = true;
            _input.ActivateInputField();

            _input.onSubmit.RemoveAllListeners();

            // 엔터로 제출
            _input.onSubmit.AddListener(_ => Submit());
        }

        if(_submitButton != null)
        {
            _submitButton.onClick.RemoveAllListeners();
            _submitButton.onClick.AddListener(Submit);
            _submitButton.interactable = true;
        }

        Debug.Log($"[UpDown] Answer : {_answer}");
    }

    private void Submit()
    {
        if (_isEnding) return; // 종료 중이면 입력 막기
        if (_input == null || _resultText == null) return;

        if(!int.TryParse(_input.text, out int guess))
        {
            _resultText.text = "숫자를 입력하세요";
            _input.ActivateInputField();
            return;
        }

        if(guess < _min || guess > _max)
        {
            _resultText.text = $"{_min}~{_max} 사이로 입력하세요.";
            _input.text = "";
            _input.ActivateInputField();
            return;
        }

        if(guess == _answer)
        {
            _resultText.text = "정답입니다! \n 잠시 후 종료됩니다.";
            BeginEnd(success: true, _exitDelay);
            return;
        }

        _remain--;
        UpdateChanceText();

        if (guess < _answer)
            _resultText.text = $"{guess}보다 UP!";
        else
            _resultText.text = $"{guess}보다 DOWN!";

        if(_remain <= 0)
        {
            _resultText.text = $"실패! 정답은 {_answer} 였습니다. \n잠시 후 종료됩니다.";
            BeginEnd(success: false, _exitDelay);
            return;
        }

        _input.text = "";
        _input.ActivateInputField();
    }

    private void BeginEnd(bool success, float delay)
    {
        _isEnding = true;
        SetInteractable(false);

        if (_exitCor != null) StopCoroutine(_exitCor);
        _exitCor = StartCoroutine(ExitAfterDelay(success, delay));
    }

    private IEnumerator ExitAfterDelay(bool success, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if(success)
        {
            CompleteMission();
        }
        else
        {
            MissionContainer.Instance.OnClickExitButton();
        }
    }

    private void SetInteractable(bool value)
    {
        if (_input != null) _input.interactable = value;
        if (_submitButton != null) _submitButton.interactable = value;
    }

    private void UpdateChanceText()
    {
        if (_chanceText != null)
            _chanceText.text = $"남은 기회 : {_remain}/{_maxTry}";
    }
}