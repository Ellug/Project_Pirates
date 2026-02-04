using UnityEngine;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(Renderer))]
public class ProximityLight : MonoBehaviour
{
    private Light _light;
    private Renderer _lightRenderer;
    private bool _currentEnabled = false;
    private LightShadows _defaultShadows;
    private bool _shadowEnabled;

    [Header("Shadow Options")]
    [SerializeField] private bool allowShadows = true;

    public bool IsActiveByPlayer { get; private set; }
    private bool _byOcclusion = false;
    private bool _byPower = true;

    void Awake()
    {
        _light = GetComponent<Light>();
        _lightRenderer = GetComponent<Renderer>();
        _defaultShadows = _light.shadows;
        _shadowEnabled = _defaultShadows != LightShadows.None;

        // 시작 시 모든 라이트 OFF
        _light.enabled = false;
        IsActiveByPlayer = false;
    }

    public void SetByPlayer(bool on)
    {
        IsActiveByPlayer = on;
        ApplyFinalState();
    }

    public void SetByOcclusion(bool on)
    {
        _byOcclusion = on;
        ApplyFinalState();
    }

    public void SetByPower(bool on)
    {
        _byPower = on;
        ApplyFinalState();
    }

    public bool CanCastShadows => allowShadows && _defaultShadows != LightShadows.None;

    public void SetShadowEnabled(bool enabled)
    {
        if (_light == null) return;

        if (!allowShadows || _defaultShadows == LightShadows.None)
            enabled = false;

        if (_shadowEnabled == enabled) return;

        _shadowEnabled = enabled;
        _light.shadows = _shadowEnabled ? _defaultShadows : LightShadows.None;
    }

    // ON/OFF 호출 최소화
    public void ApplyFinalState()
    {
        bool shouldEnable = _byPower && (IsActiveByPlayer || _byOcclusion);

        if (_currentEnabled == shouldEnable) return;

        _currentEnabled = shouldEnable;
        _light.enabled = _currentEnabled;
    }

    // 카메라 시야 검사 (Bounds 기준, Plane[] 사용)
    public bool IsVisibleFromCamera(Plane[] frustumPlanes)
    {
        if (_lightRenderer == null) return false;
        return GeometryUtility.TestPlanesAABB(frustumPlanes, _lightRenderer.bounds);
    }
}
