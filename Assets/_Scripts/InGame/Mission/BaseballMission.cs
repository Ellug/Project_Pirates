using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BaseballMission : MissionBase
{
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _roundText;
    [SerializeField] private TMP_InputField[] _numInput;
    
    private List<int> _answer = new List<int>();
    private int _currentRound = 1;
    private int _maxRound = 9;

    public override void Init()
    {
        _roundText.text = "Round 1";
        _resultText.text = "○ : 0 | △ : 0 | X : 0";

        for (int i = 0; i < _numInput.Length; i++)
        {
            _numInput[i].text = "0";
            _numInput[i].interactable = false;
        }

        _answer.Clear();
        while (_answer.Count < 3)
        {
            int rdNum = Random.Range(0, 10);

            if (_answer.Contains(rdNum) == false)
                _answer.Add(rdNum);
        }
    }

    public void UpNum1() => ChangeNumber(0, 1);
    public void DownNum1() => ChangeNumber(0, -1);

    public void UpNum2() => ChangeNumber(1, 1);
    public void DownNum2() => ChangeNumber(1, -1);

    public void UpNum3() => ChangeNumber(2, 1);
    public void DownNum3() => ChangeNumber(2, -1);

    private void ChangeNumber(int index, int amount)
    {
        if (int.TryParse(_numInput[index].text, out int currentNum))
        {
            int nextNum = currentNum + amount;

            if (nextNum > 9) nextNum = 0;
            if (nextNum < 0) nextNum = 9;

            _numInput[index].text = nextNum.ToString();
        }
    }

    public void OnClickSubmit()
    {
        int strike = 0;
        int ball = 0;

        for (int i = 0; i < _numInput.Length; i++)
        {
            int inputVal = int.Parse(_numInput[i].text);

            if (inputVal == _answer[i])
                strike++;
            else if (_answer.Contains(inputVal)) 
                ball++;
        }

        ProcessResult(strike, ball);
    }

    private void ProcessResult(int strike, int ball)
    {
        if (strike == 3)
        {
            _resultText.text = "MISSION CLEAR!";
            CompleteMission();
        }
        else
        {
            _resultText.text = $"○ : {strike} | △ : {ball} | X : {3 - strike - ball}";

            if (_currentRound >= _maxRound)
            {
                FailRoutine();
            }
            else
            {
                _currentRound++;
                _roundText.text = $"Round {_currentRound}";
            }
        }
    }

    private IEnumerator FailRoutine()
    {
        _resultText.text = "<color=red>미션 실패!</color>";
        yield return new WaitForSeconds(1.5f);

        _resultText.text = "게임을 다시 시작합니다...";
        yield return new WaitForSeconds(1.0f);

        Init();
    }
}
