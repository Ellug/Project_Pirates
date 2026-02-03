using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    [Tooltip("프러스텀 기준 좌우 시야를 추가로 늘리거나 줄이는 각도 (±n도)")]
    public float horizontalFOVOffset = 0f; // 0이면 카메라 기본 시야, 30이면 좌우 30도 더 넓음

    [Tooltip("컬링 체크 간격 (초)")]
    public float checkInterval = 0.1f;

    private Camera _playerCamera;
    private List<ProximityLight> allLights = new List<ProximityLight>();

    void Awake()
    {
        // 씬 시작 시 라이트 수집
        allLights.AddRange(FindObjectsByType<ProximityLight>(FindObjectsSortMode.None));

        // 자신에게 할당된 플레이어 캐릭터 카메라만 가져오기
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

    // TODO: 업데이트 컬링 호출마다 배열 할당하는 거 메모리 낭비?? Plane[] 캐시로 재사용 하는게 낫지 않은지 확인 요망
    private void UpdateCulling()
    {
        if (_playerCamera == null || playerHead == null) return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);

        foreach (var light in allLights)
        {
            if (light == null) continue;

            // 플레이어 근접 Sphere 체크
            bool inProximity = Vector3.Distance(light.transform.position, playerHead.position) <= proximityRadius;

            // 카메라 거리 체크
            float distance = Vector3.Distance(light.transform.position, _playerCamera.transform.position);
            bool inDistance = distance >= minCameraDistance && distance <= maxCameraDistance;

            // 카메라 프러스텀 컬링
            bool inCameraView = GeometryUtility.TestPlanesAABB(planes, light.GetComponent<Renderer>().bounds);

            // 좌우 시야 각도 조정 (기본 FOV + horizontalFOVOffset)
            Vector3 dirToLight = (light.transform.position - _playerCamera.transform.position).normalized;
            float angle = Vector3.Angle(_playerCamera.transform.forward, dirToLight);

            float cameraFOVHorizontal = CameraVerticalToHorizontalFOV(_playerCamera.fieldOfView, _playerCamera.aspect);
            bool inHorizontalFOV = angle <= (cameraFOVHorizontal * 0.5f + horizontalFOVOffset);

            // 최종 컬링 적용
            bool shouldBeActive = inDistance && inCameraView && inHorizontalFOV;

            light.SetByOcclusion(shouldBeActive);
            light.SetByPlayer(inProximity);
            light.ApplyFinalState();
        }
    }

    // 유니티 카메라 수직 FOV를 수평 FOV로 변환
    private float CameraVerticalToHorizontalFOV(float verticalFOV, float aspect)
    {
        float radV = verticalFOV * Mathf.Deg2Rad;
        float radH = 2f * Mathf.Atan(Mathf.Tan(radV / 2f) * aspect);
        return radH * Mathf.Rad2Deg;
    }
}
