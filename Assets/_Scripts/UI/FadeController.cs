using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private GameObject _fadeCanvas;
    [SerializeField] private Image _background;
    [SerializeField] private Image _lightEffect;
    [SerializeField] private TextMeshProUGUI _mainText;
    [SerializeField] private TextMeshProUGUI _subText;

    [SerializeField] private float _fadeSpeed = 1f;

    void Start()
    {
        GameManager.Instance.SetFadingController(this);
    }
    public void StartInGameFade(bool isMafia, BaseJob jopType)
    {
        ResetState();
        PlaySpotlightSequence(isMafia, jopType);
    }

    public void EndInGameFade(bool isWin)
    {
        ResetState();
        EndSpotlightSequence(isWin);
    }
    private void PlaySpotlightSequence(bool isMafia, BaseJob jopType)
    {
        if (isMafia == true)
        {
            _mainText.text = "당신은 마피아 입니다.";
            _lightEffect.color = new Color(255, 0, 0);
        }
        else
        {
            _mainText.text = "당신은 시민 입니다.";
            _lightEffect.color = new Color(0, 255, 0);
        }

        _subText.text = $"당신의 직업은 {jopType.name} 입니다.";//뒤에 jopType.infomation 등을 통해 내용도 출력하면 좋을듯?

        StartCoroutine(Fading());
    }

    private void EndSpotlightSequence(bool isWin)
    {
        if (isWin == true)
        {
            _mainText.text = "승리";
            _lightEffect.color = new Color(0, 255, 0);
        }
        else
        {
            _mainText.text = "패배";
            _lightEffect.color = new Color(255, 0, 0);
        }

        StartCoroutine(Fading());
        InGameManager.ExitForLocal();
    }
    IEnumerator Fading()
    {
        float alpha = 0f;
        while (alpha < 1.0f)
        {
            alpha += Time.deltaTime * _fadeSpeed;
            SetAlpha(_lightEffect, alpha);
            SetAlpha(_mainText, alpha);
            SetAlpha(_subText, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(3.0f);

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * _fadeSpeed;
            SetAlpha(_lightEffect, alpha);
            SetAlpha(_mainText, alpha);
            SetAlpha(_subText, alpha);
            yield return null;
        }

        float panelAlpha = 1.0f;
        while (panelAlpha > 0f)
        {
            panelAlpha -= Time.deltaTime * _fadeSpeed;
            SetAlpha(_background, panelAlpha);
            yield return null;
        }

        _fadeCanvas.SetActive(false);
    }

    private void ResetState()
    {
        _fadeCanvas.SetActive(true);
        SetAlpha(_background, 1f);
        SetAlpha(_lightEffect, 0f);
        SetAlpha(_mainText, 0f);
        SetAlpha(_subText, 0f);
        _mainText.text = string.Empty;
        _subText.text = string.Empty;
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(alpha);
        img.color = c;
    }

    private void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = Mathf.Clamp01(alpha);
        text.color = c;
    }
}
