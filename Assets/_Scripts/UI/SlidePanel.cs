using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class SlidePanel : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private float _hiddenX = 600f;
    [SerializeField] private float _shownX = 0f;

    private bool _isOpen;
    private Tween _tween;

    private void Reset()
    {
        _panel = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (_panel == null) _panel = GetComponent<RectTransform>();

        _panel.anchoredPosition = new Vector2(_hiddenX, _panel.anchoredPosition.y);
        _isOpen = false;
    }

    public void Toggle()
    {
        SetOpen(!_isOpen);
    }

    public void SetOpen(bool open)
    {
        _isOpen = open;

        _tween?.Kill();

        float targetX = _isOpen ? _shownX : _hiddenX;
        _tween = _panel.DOAnchorPosX(targetX, _duration)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(true);
    }
}
