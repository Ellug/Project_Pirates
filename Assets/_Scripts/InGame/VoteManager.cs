using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteManager : MonoBehaviour
{
    public static VoteManager Instance { get; private set; }

    [Header("Alert Panels")]
    [SerializeField] private GameObject _DeadBodyAlertPanel;
    [SerializeField] private GameObject _CenterAlertPanel;
    [SerializeField] private GameObject _votePanel;

    [Header("Teleport Area")]
    [SerializeField] private Transform _teleportArea;
    [SerializeField] private float _areaRangeX = 5f;
    [SerializeField] private float _areaRangeZ = 5f;
    [SerializeField] private float _minDistance = 1.5f;

    [Header("Timing")]
    [SerializeField] private float _teleportDelay = 1f;
    [SerializeField] private float _panelFadeOutDelay = 3f;

    private List<Vector3> _usedPositions = new();

    private void Awake()
    {
        Instance = this;
    }

    // RPC로 호출됨 (PlayerManager에서)
    public void OnDeadBodyReported()
    {
        StartCoroutine(TeleportSequence(true));
    }

    public void OnCenterReported()
    {
        StartCoroutine(TeleportSequence(false));
    }

    private IEnumerator TeleportSequence(bool isDeadBody)
    {
        // 알림 패널 표시
        if (isDeadBody)
            _DeadBodyAlertPanel.SetActive(true);
        else
            _CenterAlertPanel.SetActive(true);
            
        Debug.Log("[VoteManager] 시체 발견! 알림 패널 표시");

        // 1초 대기
        yield return new WaitForSeconds(_teleportDelay);

        // 로컬 플레이어 텔레포트
        TeleportLocalPlayer();

        // 2초 뒤 패널 비활성화
        yield return new WaitForSeconds(_panelFadeOutDelay);

        HideAllAlertPanel();

        // 사용된 위치 초기화
        _usedPositions.Clear();
    }

    private void TeleportLocalPlayer()
    {
        if (_teleportArea == null)
        {
            Debug.LogWarning("[VoteManager] 텔레포트 영역이 어디감?");
            return;
        }

        GameObject localPlayer = PlayerController.LocalInstancePlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[VoteManager] 로컬 플레이어 어디감?");
            return;
        }

        // TODO: 여기서 로컬 플레이어가 살아있는지 어떤지 판단해서 추가 로직

        Vector3 randomPos = GetRandomNonOverlappingPosition();
        localPlayer.transform.position = randomPos;

        Debug.Log($"[VoteManager] 플레이어 텔레포트 완료: {randomPos}");
    }

    // 겹치지 않은 랜덤 위치 선정
    private Vector3 GetRandomNonOverlappingPosition()
    {
        if (_teleportArea == null)
            return Vector3.zero;

        Vector3 areaCenter = _teleportArea.position;

        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(-_areaRangeX, _areaRangeX);
            float randomZ = Random.Range(-_areaRangeZ, _areaRangeZ);
            Vector3 candidate = areaCenter + new Vector3(randomX, 0f, randomZ);

            bool isOverlapping = false;
            foreach (Vector3 usedPos in _usedPositions)
            {
                if (Vector3.Distance(candidate, usedPos) < _minDistance)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                _usedPositions.Add(candidate);
                return candidate;
            }
        }

        // 모든 시도 실패 시 랜덤 위치 반환
        Vector3 fallback = areaCenter + new Vector3(
            Random.Range(-_areaRangeX, _areaRangeX),
            0f,
            Random.Range(-_areaRangeZ, _areaRangeZ)
        );
        _usedPositions.Add(fallback);
        return fallback;
    }


    public void HideAllAlertPanel()
    {
        _DeadBodyAlertPanel.SetActive(false);
        _CenterAlertPanel.SetActive(false);
    }
}
