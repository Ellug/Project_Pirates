using System.Collections;
using UnityEngine;

public class MiniMapRenderer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform _mapRect;

    [Header("Bounds Source")]
    [SerializeField] private BoxCollider _boundsCollider;

    [Header("Mapping")]
    [SerializeField] private float _padding = 8f;

    [Header("Player Marker")]
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private Transform _playerTarget;

    private Bounds _worldBounds;
    private Vector3 _worldCenter;
    private float _scale;
    private bool _built;

    void Start()
    {
        StartCoroutine(BuildDelayed());
    }

    private IEnumerator BuildDelayed()
    {
        yield return null;
        Build();
    }

    void OnEnable()
    {
        if (_built)
            UpdatePlayerMarker();
    }

    void Update()
    {
        if (!_built) return;

        UpdatePlayerMarker();
    }

    public void Build()
    {
        if (_mapRect == null)
        {
            Debug.LogWarning("[MiniMapRenderer] Map Rect is missing.");
            return;
        }

        EnsureMarkerSetup();

        if (!TryGetWorldBounds(out _worldBounds))
        {
            Debug.LogWarning("[MiniMapRenderer] World bounds not found. Assign a Bounds Collider.");
            return;
        }

        Canvas.ForceUpdateCanvases();

        var rect = _mapRect.rect;
        float mapWidth = Mathf.Max(1f, rect.width - (_padding * 2f));
        float mapHeight = Mathf.Max(1f, rect.height - (_padding * 2f));
        float worldWidth = Mathf.Max(0.001f, _worldBounds.size.x);
        float worldHeight = Mathf.Max(0.001f, _worldBounds.size.z);

        _scale = Mathf.Min(mapWidth / worldWidth, mapHeight / worldHeight);
        _worldCenter = _worldBounds.center;

        _built = true;
        UpdatePlayerMarker();
    }

    private bool TryGetWorldBounds(out Bounds bounds)
    {
        bounds = default;
        if (_boundsCollider == null)
            return false;

        bounds = _boundsCollider.bounds;
        return true;
    }

    private void UpdatePlayerMarker()
    {
        if (_playerMarker == null)
            return;

        EnsureMarkerSetup();

        if (_playerTarget != null)
        {
            var controller = _playerTarget.GetComponent<PlayerController>();
            if (controller != null && controller.photonView != null && !controller.photonView.IsMine)
                _playerTarget = null;
        }

        if (_playerTarget == null)
            TryResolvePlayer();

        if (_playerTarget == null) return;

        var model = _playerTarget.GetComponent<PlayerModel>();
        bool isDead = model != null && model.IsDead;
        if (_playerMarker.gameObject.activeSelf == isDead)
            _playerMarker.gameObject.SetActive(!isDead);

        if (isDead)
            return;

        _playerMarker.anchoredPosition = WorldToMap(_playerTarget.position);
    }

    private Vector2 WorldToMap(Vector3 worldPos)
    {
        return new Vector2(
            (worldPos.x - _worldCenter.x) * _scale,
            (worldPos.z - _worldCenter.z) * _scale
        );
    }

    private void EnsureMarkerSetup()
    {
        if (_playerMarker == null || _mapRect == null)
            return;

        if (_playerMarker.parent != _mapRect)
            _playerMarker.SetParent(_mapRect, false);

        if (_playerMarker.anchorMin != new Vector2(0.5f, 0.5f) ||
            _playerMarker.anchorMax != new Vector2(0.5f, 0.5f) ||
            _playerMarker.pivot != new Vector2(0.5f, 0.5f))
        {
            _playerMarker.anchorMin = new Vector2(0.5f, 0.5f);
            _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _playerMarker.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private void TryResolvePlayer()
    {
        if (_playerTarget != null) return;

        if (PlayerController.LocalInstancePlayer != null)
        {
            var localController = PlayerController.LocalInstancePlayer.GetComponent<PlayerController>();
            if (localController != null && localController.photonView != null && localController.photonView.IsMine)
            {
                _playerTarget = localController.transform;
                return;
            }
        }

        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null && controller.photonView != null && controller.photonView.IsMine)
            _playerTarget = controller.transform;
    }
}
