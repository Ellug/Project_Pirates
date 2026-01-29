using TMPro;
using UnityEngine;

public class MathMission : MissionBase
{
    [SerializeField] private TextMeshProUGUI _problem;
    [SerializeField] private TMP_InputField _answerInput;
    [SerializeField] private TextMeshProUGUI _result;

    private string _answer;

    void Start()
    {
        
    }

    public override void Init()
    {
        _result.gameObject.SetActive(false);
        int first = Random.Range(2, 10);
        int second = Random.Range(2, 10);
        int third = Random.Range(2, 10);
        int answer = first + second * third;
        _answer = answer.ToString();
        _problem.text = $"{first} + {second} * {third} = ?";
    }

    public void OnClickSubmit()
    {
        if ( _answerInput.text == _answer )
        {
            CompleteMission();
        }
    }
}
