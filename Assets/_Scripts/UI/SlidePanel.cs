using UnityEngine;
using DG.Tweening;

public class SlidePanel : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _duration = 0.25f;

    private float _shownX;
    private float _hiddenX;

    private bool _isOpen;
    private Tween _tween;

    private void Reset()
    {
        _panel = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (_panel == null) _panel = GetComponent<RectTransform>();

        // 보이는 위치를 기준으로 저장
        _shownX = _panel.anchoredPosition.x;

        // 패널 폭만큼 빼기
        _hiddenX = _shownX - _panel.rect.width;

        var p = _panel.anchoredPosition;
        p.x = _hiddenX;
        _panel.anchoredPosition = p;

        _isOpen = false;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnToggleSlidePanelUI += Toggle;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnToggleSlidePanelUI -= Toggle;
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;

        _tween?.Kill();

        float targetX = _isOpen ? _shownX : _hiddenX;
        _tween = _panel.DOAnchorPosX(targetX, _duration)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(true);
    }
}
