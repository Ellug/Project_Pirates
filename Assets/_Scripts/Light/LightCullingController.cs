using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

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

    [Header("Stabilization (Flicker Fix)")]
    [Tooltip("offDelay보다 작은 값이면 무시됩니다. (최소 유지 시간)")]
    public float minHoldTime = 0.3f;
    [Tooltip("켜진 상태에서만 추가로 늘려주는 근접 반경")]
    public float proximityHysteresis = 2.0f;
    [Tooltip("켜진 상태에서만 추가로 늘려주는 거리 범위")]
    public float distanceHysteresis = 2.0f;
    [Tooltip("켜진 상태에서만 추가로 늘려주는 FOV 여유값")]
    public float fovHysteresis = 5.0f;

    [Header("Shadow Culling")]
    public bool limitAdditionalLightShadows = true;
    public int maxShadowedLights = 12;
    public float shadowDistance = 15f;

    [Header("Gizmo (Editor only)")]
    public bool showProximityGizmo = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.7f);

    private Camera _playerCamera;
    private List<ProximityLight> allLights = new List<ProximityLight>();
    private Plane[] _frustumPlanes = new Plane[6];
    private Dictionary<ProximityLight, Renderer> _lightRendererMap = new Dictionary<ProximityLight, Renderer>(64);
    private Dictionary<ProximityLight, float> _lightExpireTimers = new Dictionary<ProximityLight, float>(64);
    private readonly List<ShadowCandidate> _shadowCandidates = new List<ShadowCandidate>(128);
    private float _lastUpdateTime;

    private struct ShadowCandidate
    {
        public ProximityLight light;
        public float distSqr;
        public ShadowCandidate(ProximityLight light, float distSqr)
        {
            this.light = light;
            this.distSqr = distSqr;
        }
    }

    void Awake()
    {
        // 플레이어 프리팹에 붙어있다면 로컬 플레이어만 실행
        var pv = GetComponentInParent<PhotonView>();
        if (pv != null && !pv.IsMine)
        {
            enabled = false;
            return;
        }

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

        // Awake 시점에는 플레이어가 아직 스폰되지 않았을 수 있음
        // UpdateCulling에서 lazy 초기화
    }

    void Start()
    {
        _lastUpdateTime = Time.time;
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
        // 플레이어 참조가 없으면 찾기 시도
        if (_playerCamera == null || !_playerCamera.isActiveAndEnabled || playerHead == null)
        {
            TryFindPlayer();
            if (_playerCamera == null || playerHead == null) return;
        }

        float now = Time.time;
        float delta = Mathf.Max(0f, now - _lastUpdateTime);
        _lastUpdateTime = now;

        float holdTime = Mathf.Max(offDelay, minHoldTime);

        GeometryUtility.CalculateFrustumPlanes(_playerCamera, _frustumPlanes);
        float shadowDistanceSqr = shadowDistance * shadowDistance;

        if (limitAdditionalLightShadows)
            _shadowCandidates.Clear();

        foreach (var light in allLights)
        {
            if (light == null) continue;

            bool currentlyOn = _lightExpireTimers.TryGetValue(light, out float remain) && remain > 0f;

            float extraDist = currentlyOn ? Mathf.Max(0f, distanceHysteresis) : 0f;
            float extraFov = currentlyOn ? Mathf.Max(0f, fovHysteresis) : 0f;
            float extraProx = currentlyOn ? Mathf.Max(0f, proximityHysteresis) : 0f;

            float maxDist = maxCameraDistance + extraDist;
            float minDist = Mathf.Max(0f, minCameraDistance - extraDist);

            // 1. 카메라 시야/거리 체크
            float distance = Vector3.Distance(light.transform.position, _playerCamera.transform.position);
            bool inDistance = distance >= minDist && distance <= maxDist;

            Vector3 dirToLight = (light.transform.position - _playerCamera.transform.position).normalized;
            float angle = Vector3.Angle(_playerCamera.transform.forward, dirToLight);
            float cameraFOVHorizontal = CameraVerticalToHorizontalFOV(_playerCamera.fieldOfView, _playerCamera.aspect);
            bool inHorizontalFOV = angle <= (cameraFOVHorizontal * 0.5f + horizontalFOVOffset + extraFov);

            Renderer rend = null;
            _lightRendererMap.TryGetValue(light, out rend);
            bool inCameraView = (rend != null) && GeometryUtility.TestPlanesAABB(_frustumPlanes, rend.bounds);

            bool isViewDetected = inDistance && inCameraView && inHorizontalFOV;

            // 2. 플레이어 근접 체크
            bool isProximityDetected;
            float proximityRadiusSqr = (proximityRadius + extraProx) * (proximityRadius + extraProx);
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
                _lightExpireTimers[light] = holdTime;
            }
            else
            {
                // 범위 밖이라면 체크 간격만큼 시간 차감
                if (_lightExpireTimers[light] > 0)
                    _lightExpireTimers[light] = Mathf.Max(0f, _lightExpireTimers[light] - delta);
            }

            // 타이머가 0보다 크면 켜진 상태 유지
            bool finalState = _lightExpireTimers[light] > 0;

            light.SetByOcclusion(isViewDetected && finalState);
            light.SetByPlayer(isProximityDetected || (finalState && !isViewDetected));
            light.ApplyFinalState();

            if (limitAdditionalLightShadows)
            {
                light.SetShadowEnabled(false);

                if (finalState && light.CanCastShadows)
                {
                    float distSqr = (light.transform.position - _playerCamera.transform.position).sqrMagnitude;
                    if (distSqr <= shadowDistanceSqr)
                        _shadowCandidates.Add(new ShadowCandidate(light, distSqr));
                }
            }
            else
            {
                light.SetShadowEnabled(finalState);
            }
        }

        if (limitAdditionalLightShadows && _shadowCandidates.Count > 0)
        {
            _shadowCandidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));

            int count = Mathf.Min(maxShadowedLights, _shadowCandidates.Count);
            for (int i = 0; i < count; i++)
                _shadowCandidates[i].light.SetShadowEnabled(true);
        }
    }

    private float CameraVerticalToHorizontalFOV(float verticalFOV, float aspect)
    {
        float radV = verticalFOV * Mathf.Deg2Rad;
        float radH = 2f * Mathf.Atan(Mathf.Tan(radV / 2f) * aspect);
        return radH * Mathf.Rad2Deg;
    }

    private void TryFindPlayer()
    {
        // LocalInstancePlayer가 있으면 사용
        if (PlayerController.LocalInstancePlayer != null)
        {
            _playerCamera = PlayerController.LocalInstancePlayer.GetComponentInChildren<Camera>();
            if (_playerCamera != null)
                playerHead = _playerCamera.transform;
        }

        // 그래도 없으면 Camera.main 폴백
        if (_playerCamera == null)
            _playerCamera = Camera.main;

        if (_playerCamera != null && playerHead == null)
            playerHead = _playerCamera.transform;
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



