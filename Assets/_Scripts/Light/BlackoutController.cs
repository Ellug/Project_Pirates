using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BlackoutController : MonoBehaviour
{
    [Header("Lighting Core")]
    [SerializeField] private LightingSwitchManager lightingManager;

    [Header("Blackout Blend Settings")]
    [Tooltip("정전 전환 시간")]
    public float blackoutTime = 2.0f;

    [Tooltip("복구 전환 시간")]
    public float restoreTime = 1.5f;

    private Coroutine currentRoutine;

    public void StartBlackout()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(BlackoutRoutine());
    }

    public void RestoreLight()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(RestoreRoutine());
    }

    private IEnumerator BlackoutRoutine()
    {
        // 1. 라이트맵 즉시 교체
        lightingManager.ApplyLightOffImmediate();

        // 2. APV 시나리오 블렌딩
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / blackoutTime;

            if (ProbeReferenceVolume.instance != null)
                ProbeReferenceVolume.instance.BlendLightingScenario(
                    lightingManager.lightOffScenario, t);

            yield return null;
        }

        currentRoutine = null;
    }

    private IEnumerator RestoreRoutine()
    {
        // 1. 라이트맵 즉시 복구
        lightingManager.ApplyLightOnImmediate();

        // 2. APV 시나리오 블렌딩
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / restoreTime;

            if (ProbeReferenceVolume.instance != null)
                ProbeReferenceVolume.instance.BlendLightingScenario(
                    lightingManager.lightOnScenario, t);

            yield return null;
        }

        currentRoutine = null;
    }
}
