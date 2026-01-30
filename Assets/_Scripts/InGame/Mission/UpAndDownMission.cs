using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpAndDownMission : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _chanceText;
    [SerializeField] private Button _submitButton;

    private int _answer;
    private int _remainChance = 7;

    //public override void Init()
    //{
    //    _answer = Random.Range(1, 101);
    //    _remainChance = 7;
    //
    //    _resultText.text = "1부터 100 사이 숫자를 입력해주세요.";
    //
    //}
}
