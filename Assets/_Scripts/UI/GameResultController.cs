//using UnityEngine;
//using TMPro;

//public class GameResultController : MonoBehaviour
//{
//    [SerializeField] TextMeshProUGUI _resultText;
//    [SerializeField] GameObject _resultPanel;

//    void Start()
//    {
//        GameManager.Instance.RegistResultPanel(this);
//        _resultPanel.SetActive(false);
//    }

//    public void Victory()
//    {
//        _resultText.text = "승리";
//        _resultPanel.SetActive(true);
//    }
//    public void Defeat()
//    {
//        _resultText.text = "패배";
//        _resultPanel.SetActive(true);
//    }
//}
