using TMPro;
using UnityEngine;

public class AjeMission : MissionBase
{
    [SerializeField] private TMP_Text _curQuestion;
    [SerializeField] private TMP_InputField _answerInput;
    [SerializeField] private TMP_Text _resultText;
    
    private TMP_Text _curAnswer;

    private readonly (string problem, string answer)[] _quizList =
    {
        ("소가 한 마리면?", "소원"),
        ("소가 두마리면?", "투우"),
        ("소가 네마리면?", "소포"),
        ("소가 다섯마리면?", "오우"),
        ("소가 지면?", "젖소"),
        ("소가 날면?", "하늘소"),
        ("소가 좋으면?", "좋소"),
        ("소가 싫으면?", "싫소"),
        ("소가 불타면?", "탄소"),
        ("소가 죽으면?", "다이소"),
        ("소가 웃으면?", "우하하"),
        ("소가 놀라면?", "음메이징"),
        ("소가 인사하면?", "반갑소"),
        ("소가 서울 가면?", "소설가"),
        ("소가 가난하면?", "우거지"),
        ("소가 군대 가면?", "육군훈련소"),
        ("소가 발이 두개면?", "이발소"),
        ("소가 계단을 오르면?", "소오름"),
        ("소가 커피를 마시면?", "에스프레소"),
        ("소가 번개를 맞으면?", "우사인볼트"),
        ("소가 노래를 부르면?", "소송"),
        ("소가 단체로 노래를 부르면?", "단체소송"),
        ("[로스트아크] 할족이 승차거부 당하면?", "타지마할"),
        ("[로스트아크] 할족이 승차요구 당하면?", "할타"),
        ("[로스트아크] 할족 자식들이 싸우는 이유는?", "상속분할"),
        ("[로스트아크] 할족이 주식투자 할때 자주 쓰는 기법은?", "분할매수"),
        ("[로스트아크] 할족의 아버지는?", "할아버지"),
        ("[로스트아크] 할족이 호크아이 클래스를 선택하면?", "할매"),
        ("[로스트아크] 할족이 못 들어가는 카페는?", "할리스커피"),
        ("[로스트아크] 할족 커플은?", "할짝"),
        ("가장 가난한 왕은?", "최저임금"),
        ("사 온다고 하면서 못 사오는 것은?", "못"),
        ("왕이 넘어지면?", "킹콩"),
        ("자가용의 반대말은?", "커용"),
        ("4가 도망가면?", "포도주"),
        ("한남에 자가 있는 가수는?", "김종국"),
        ("치아가 잘 보이는 사람은?", "이보영"),
        ("씨엘의 엉덩이를 영어로?", "class"),
    };

    public override void Init()
    {
        _resultText.text = "";

        int index = Random.Range(0, _quizList.Length);
        _curQuestion.text = _quizList[index].problem;
        _curAnswer.text = _quizList[index].answer;
        _answerInput.text = "";

        _curAnswer.gameObject.SetActive(false);

        // Enter 키 이벤트 구독
        if (InputManager.Instance != null)
            InputManager.Instance.OnSubmitUI += OnClickSubmit;
    }

    private void OnDestroy()
    {
        // Enter 키 이벤트 구독 해제
        if (InputManager.Instance != null)
            InputManager.Instance.OnSubmitUI -= OnClickSubmit;
    }

    public void OnClickSubmit()
    {
        _curAnswer.gameObject.SetActive(true);

        if (_answerInput.text != _curAnswer.text)
        {
            _resultText.text = $"오답입니다ㅋ 정답은 {_curAnswer.text} 깔깔깔";
            MissionContainer.Instance.CloseMissionPanel();
            return;
        }

        _resultText.text = "이걸 맞추네...";
        CompleteMission();
    }
}
