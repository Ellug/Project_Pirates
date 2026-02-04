using UnityEngine;
using DG.Tweening;
using TMPro;

public class SlidePanel : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _duration = 0.25f;

    [Header("InfoText")]
    [SerializeField] private TextMeshProUGUI _jobName;
    [SerializeField] private TextMeshProUGUI _jobInfo;
    [SerializeField] private TextMeshProUGUI _firstItemInfo;
    [SerializeField] private TextMeshProUGUI _secondItemInfo;

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
    public void UpdateInformation(BaseJob job, ItemData[] items)
    {
        if (job != null)
        {
            _jobName.text = $"[{job.name}]";
            _jobInfo.text = job.jobInformation;
        }
        else
        {
            // job이 null(무직)일 때 표시될 내용
            _jobName.text = "[회사원]";
            _jobInfo.text = "아무런 능력이 없습니다.";
        }

        _firstItemInfo.text = (items != null && items.Length > 0 && items[0] != null)
            ? $"[{items[0].itemName}] : {items[0].itemInformation}"
            : "[비어 있음]";

        _secondItemInfo.text = (items != null && items.Length > 1 && items[1] != null)
            ? $"[{items[1].itemName}] : {items[1].itemInformation}"
            : "[비어 있음]";
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;

        if (_isOpen)
        {
            if (PlayerController.LocalInstancePlayer != null)
            {
                var model = PlayerController.LocalInstancePlayer.GetComponent<PlayerModel>();
                if (model != null)
                {
                    UpdateInformation(model.MyJob, model.GetInventory());
                }
            }
        }
        _tween?.Kill();

        float targetX = _isOpen ? _shownX : _hiddenX;
        _tween = _panel.DOAnchorPosX(targetX, _duration)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(true);
    }
}
