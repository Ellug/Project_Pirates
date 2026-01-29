using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightCullingController : MonoBehaviour
{
    [Header("Player Head Reference")]
    public Transform playerHead;

    [Header("컬링 옵션")]
    [Tooltip("플레이어 머리 기준 근접 반경")]
    public float proximityRadius = 12f;

    [Tooltip("카메라 시야 컬링 최소 거리")]
    public float minCameraDistance = 0f;

    [Tooltip("카메라 시야 컬링 최대 거리")]
    public float maxCameraDistance = 30f;

    [Tooltip("컬링 체크 간격 (초)")]
    public float checkInterval = 0.1f;

    private Camera _playerCamera;
    private List<ProximityLight> allLights = new List<ProximityLight>();

    private void Awake()
    {
        // 씬 시작 시 라이트 수집
        allLights.AddRange(FindObjectsByType<ProximityLight>(FindObjectsSortMode.None));

        // 로컬 플레이어 카메라 자동 참조
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.photonView.IsMine)
            {
                _playerCamera = p.GetComponentInChildren<Camera>();
                if (playerHead == null)
                    playerHead = _playerCamera?.transform;
                break;
            }
        }

        if (_playerCamera == null)
            _playerCamera = Camera.main;

        if (playerHead == null)
            Debug.LogError("[LightCullingController] PlayerHead를 찾지 못했습니다!");
    }

    private void Start()
    {
        // Coroutine으로 주기적 컬링 체크
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

        // 카메라 시야 프러스텀 한 번만 계산
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);

        foreach (var light in allLights)
        {
            if (light == null) continue;

            // 플레이어 근접 Sphere 체크
            bool inProximity = Vector3.Distance(light.transform.position, playerHead.position) <= proximityRadius;

            // 카메라 시야 + 거리 체크
            float distance = Vector3.Distance(light.transform.position, _playerCamera.transform.position);
            bool inCameraView = distance >= minCameraDistance && distance <= maxCameraDistance &&
                                light.IsVisibleFromCamera(planes);

            // 컬링 적용
            light.SetByOcclusion(inCameraView);
            light.SetByPlayer(inProximity);
            light.ApplyFinalState();
        }
    }
}
