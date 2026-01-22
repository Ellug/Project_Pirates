using UnityEngine;
using TMPro;

public class GameResultController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _resultText;
    void Start()
    {
        GameManager.Instance.RegistResultPanel(this);
        _resultText.gameObject.SetActive(false);
    }

    public void Victory()
    {
        _resultText.text = "승리";
        _resultText.gameObject.SetActive(true);
    }
    public void Defeat()
    {
        _resultText.text = "패배";
        _resultText.gameObject.SetActive(true);
    }
}
