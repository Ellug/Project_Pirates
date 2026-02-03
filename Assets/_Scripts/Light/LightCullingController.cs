using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LightCullingController : MonoBehaviour
{
    [Header("Player Head Reference")]
    public Transform playerHead;

    [Header("컬링 옵션")]
    [Tooltip("플레이어 머리 기준 근접 반경 (인스펙터에서만 조절)")]
    public float proximityRadius = 12f;

    [Tooltip("카메라 시야 컬링 최소 거리")]
    public float minCameraDistance = 0f;

    [Tooltip("카메라 시야 컬링 최대 거리")]
    public float maxCameraDistance = 30f;

    [Tooltip("프러스텀 기준 좌우 시야를 추가로 늘리거나 줄이는 각도 (±n도)")]
    public float horizontalFOVOffset = 0f; // 0이면 카메라 기본 시야, 30이면 좌우 30도 더 넓음

    [Tooltip("컬링 체크 간격 (초)")]
    public float checkInterval = 0.1f;

    [Header("Gizmo (Editor only)")]
    public bool showProximityGizmo = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.7f);

    private Camera _playerCamera;
    private List<ProximityLight> allLights = new List<ProximityLight>();
    private Plane[] _frustumPlanes = new Plane[6];

    private Dictionary<ProximityLight, Renderer> _lightRendererMap = new Dictionary<ProximityLight, Renderer>(64);

    void Awake()
    {
        allLights.AddRange(FindObjectsByType<ProximityLight>(FindObjectsSortMode.None));

        foreach (var light in allLights)
        {
            if (light == null) continue;
            var rend = light.GetComponent<Renderer>();
            if (!_lightRendererMap.ContainsKey(light))
                _lightRendererMap.Add(light, rend);
        }

        var localPlayer = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                          .FirstOrDefault(p => p.photonView != null && p.photonView.IsMine);

        if (localPlayer != null)
        {
            _playerCamera = localPlayer.GetComponentInChildren<Camera>();
            if (playerHead == null && _playerCamera != null)
                playerHead = _playerCamera.transform;
        }

        if (_playerCamera == null)
            _playerCamera = Camera.main;

        if (playerHead == null)
            Debug.LogError("[LightCullingController] PlayerHead를 찾지 못했습니다!");
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

            float distance = Vector3.Distance(light.transform.position, _playerCamera.transform.position);
            bool inDistance = distance >= minCameraDistance && distance <= maxCameraDistance;

            Vector3 dirToLight = (light.transform.position - _playerCamera.transform.position).normalized;
            float angle = Vector3.Angle(_playerCamera.transform.forward, dirToLight);
            float cameraFOVHorizontal = CameraVerticalToHorizontalFOV(_playerCamera.fieldOfView, _playerCamera.aspect);
            bool inHorizontalFOV = angle <= (cameraFOVHorizontal * 0.5f + horizontalFOVOffset);

            Renderer rend = null;
            _lightRendererMap.TryGetValue(light, out rend);
            bool inCameraView = (rend != null) && GeometryUtility.TestPlanesAABB(_frustumPlanes, rend.bounds);

            bool shouldBeActiveByOcclusion = inDistance && inCameraView && inHorizontalFOV;
            bool inPlayerProximity;
            if (rend != null)
            {
                float sqr = rend.bounds.SqrDistance(playerHead.position);
                inPlayerProximity = sqr <= proximityRadiusSqr;
            }
            else
            {
                inPlayerProximity = (light.transform.position - playerHead.position).sqrMagnitude <= proximityRadiusSqr;
            }

            light.SetByOcclusion(shouldBeActiveByOcclusion);
            light.SetByPlayer(inPlayerProximity);
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
        if (!showProximityGizmo) return;
        if (playerHead == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(playerHead.position, proximityRadius);
    }
#endif
}
