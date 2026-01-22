using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HowToPlayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private GameObject[] _pageList;

    private int _pageIndex = 0;
    void Start()
    {
        UpdateUI();
    }

    public void OnClickPageBackButton()
    {
        if (_pageIndex > 0)
        {
            _pageIndex--;
            UpdateUI();
        }
    }

    public void OnClickPageNextButton()
    {
        if (_pageIndex < _pageList.Length - 1)
        {
            _pageIndex++;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < _pageList.Length; i++)
        {
            _pageList[i].SetActive(i == _pageIndex);
        }

        if (_pageText != null)
        {
            _pageText.text = $"{_pageIndex + 1} / {_pageList.Length}";
        }
    }
}
