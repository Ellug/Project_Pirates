using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LightCullingController : MonoBehaviour
{
    [Header("Player Head Reference")]
    public Transform playerHead;

    [Header("컬링 옵션")]
    public float proximityRadius = 12f;
    public float minCameraDistance = 0f;
    public float maxCameraDistance = 30f;
    public float horizontalFOVOffset = 0f;
    public float checkInterval = 0.1f;

    [Tooltip("범위 밖으로 나간 후 불이 꺼질 때까지의 대기 시간")]
    public float offDelay = 2.0f;

    [Header("Gizmo (Editor only)")]
    public bool showProximityGizmo = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.7f);

    private Camera _playerCamera;
    private List<ProximityLight> allLights = new List<ProximityLight>();
    private Plane[] _frustumPlanes = new Plane[6];
    private Dictionary<ProximityLight, Renderer> _lightRendererMap = new Dictionary<ProximityLight, Renderer>(64);
    private Dictionary<ProximityLight, float> _lightExpireTimers = new Dictionary<ProximityLight, float>(64);

    void Awake()
    {
        allLights.AddRange(FindObjectsByType<ProximityLight>(FindObjectsSortMode.None));

        foreach (var light in allLights)
        {
            if (light == null) continue;
            var rend = light.GetComponent<Renderer>();
            if (!_lightRendererMap.ContainsKey(light))
                _lightRendererMap.Add(light, rend);

            // 타이머 초기화
            _lightExpireTimers[light] = 0f;
        }

        // 플레이어 검색 로직
        var localPlayer = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                          .FirstOrDefault(p => p.photonView != null && p.photonView.IsMine);

        if (localPlayer != null)
        {
            _playerCamera = localPlayer.GetComponentInChildren<Camera>();
            if (playerHead == null && _playerCamera != null)
                playerHead = _playerCamera.transform;
        }

        if (_playerCamera == null) _playerCamera = Camera.main;
    }

    void Start()
    {
        StartCoroutine(LightCullingLoop());
    }

    private IEnumerator LightCullingLoop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            UpdateCulling();
            yield return wait;
        }
    }

    private void UpdateCulling()
    {
        if (_playerCamera == null || playerHead == null) return;

        GeometryUtility.CalculateFrustumPlanes(_playerCamera, _frustumPlanes);
        float proximityRadiusSqr = proximityRadius * proximityRadius;

        foreach (var light in allLights)
        {
            if (light == null) continue;

            // 1. 카메라 시야/거리 체크
            float distance = Vector3.Distance(light.transform.position, _playerCamera.transform.position);
            bool inDistance = distance >= minCameraDistance && distance <= maxCameraDistance;

            Vector3 dirToLight = (light.transform.position - _playerCamera.transform.position).normalized;
            float angle = Vector3.Angle(_playerCamera.transform.forward, dirToLight);
            float cameraFOVHorizontal = CameraVerticalToHorizontalFOV(_playerCamera.fieldOfView, _playerCamera.aspect);
            bool inHorizontalFOV = angle <= (cameraFOVHorizontal * 0.5f + horizontalFOVOffset);

            Renderer rend = null;
            _lightRendererMap.TryGetValue(light, out rend);
            bool inCameraView = (rend != null) && GeometryUtility.TestPlanesAABB(_frustumPlanes, rend.bounds);

            bool isViewDetected = inDistance && inCameraView && inHorizontalFOV;

            // 2. 플레이어 근접 체크
            bool isProximityDetected;
            if (rend != null)
            {
                float sqr = rend.bounds.SqrDistance(playerHead.position);
                isProximityDetected = sqr <= proximityRadiusSqr;
            }
            else
            {
                isProximityDetected = (light.transform.position - playerHead.position).sqrMagnitude <= proximityRadiusSqr;
            }

            // 3. 타이머 로직 적용
            if (isViewDetected || isProximityDetected)
            {
                _lightExpireTimers[light] = offDelay;
            }
            else
            {
                // 범위 밖이라면 체크 간격만큼 시간 차감
                if (_lightExpireTimers[light] > 0)
                    _lightExpireTimers[light] -= checkInterval;
            }

            // 타이머가 0보다 크면 켜진 상태 유지
            bool finalState = _lightExpireTimers[light] > 0;

            light.SetByOcclusion(isViewDetected && finalState);
            light.SetByPlayer(isProximityDetected || (finalState && !isViewDetected));
            light.ApplyFinalState();
        }
    }

    private float CameraVerticalToHorizontalFOV(float verticalFOV, float aspect)
    {
        float radV = verticalFOV * Mathf.Deg2Rad;
        float radH = 2f * Mathf.Atan(Mathf.Tan(radV / 2f) * aspect);
        return radH * Mathf.Rad2Deg;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showProximityGizmo || playerHead == null) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(playerHead.position, proximityRadius);
    }
#endif
}