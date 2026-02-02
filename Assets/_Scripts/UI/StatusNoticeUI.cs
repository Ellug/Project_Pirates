using TMPro;
using UnityEngine;
using System.Collections;

public class StatusNoticeUI : MonoBehaviour
{
    public static StatusNoticeUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text _infoText; // 무슨 일이 일어났는지 표시
    [SerializeField] private TMP_Text _descText; // 진행상황

    private Coroutine _hideCor;
    private Coroutine _countdownCor;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    public void ShowMessage(string info, string desc = "", float duration = 3f)
    {
        StopAllRunning();

        if (_infoText != null)
        {
            _infoText.gameObject.SetActive(true);
            _infoText.text = info;
        }

        if (_descText != null)
        {
            bool hasDesc = !string.IsNullOrEmpty(desc);
            _descText.gameObject.SetActive(hasDesc);
            if (hasDesc) _descText.text = desc;
        }

        _hideCor = StartCoroutine(Co_HideAfter(duration));
    }

    public void ShowCountdown(string info, float seconds)
    {
        StopAllRunning();

        if (_infoText != null)
        {
            _infoText.gameObject.SetActive(true);
            _infoText.text = info;
        }

        if (_descText != null)
        {
            _descText.gameObject.SetActive(true);
        }

        _countdownCor = StartCoroutine(Co_Countdown(seconds));
    }

    public void HideImmediate()
    {
        StopAllRunning();

        if (_infoText != null) _infoText.gameObject.SetActive(false);
        if (_descText != null) _descText.gameObject.SetActive(false);
    }

    private IEnumerator Co_HideAfter(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        HideImmediate();
    }

    private IEnumerator Co_Countdown(float seconds)
    {
        float remain = seconds;

        while (remain > 0f)
        {
            remain -= Time.unscaledDeltaTime;

            if (_descText != null)
            {
                int s = Mathf.CeilToInt(remain);
                _descText.text = $"{s}초";
            }

            yield return null;
        }

        HideImmediate();
    }

    private void StopAllRunning()
    {
        if (_hideCor != null) { StopCoroutine(_hideCor); _hideCor = null; }
        if (_countdownCor != null) { StopCoroutine(_countdownCor); _countdownCor = null; }
    }
}