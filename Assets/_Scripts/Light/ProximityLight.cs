using UnityEngine;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(Renderer))]
public class ProximityLight : MonoBehaviour
{
    private Light _light;
    private Renderer _lightRenderer;
    private bool _currentEnabled = false;

    public bool IsActiveByPlayer { get; private set; }
    private bool _byOcclusion = false;
    private bool _byPower = true;

    void Awake()
    {
        _light = GetComponent<Light>();
        _lightRenderer = GetComponent<Renderer>();

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
