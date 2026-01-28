using UnityEngine;

public class RealtimeLightGroup : MonoBehaviour
{
    [Header("Auto Register")]
    [Tooltip("true면 씬의 모든 Light를 자동 수집")]
    public bool autoCollect = true;

    [Tooltip("비활성 오브젝트 포함")]
    public bool includeInactive = false;

    private Light[] lights;

    private void Awake()
    {
        if (autoCollect)
            CollectLights();
    }

    private void CollectLights()
    {
        if (includeInactive)
            lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        else
            lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        Debug.Log($"[RealtimeLightGroup] Collected {lights.Length} lights.");
    }
    public void SetLights(bool on)
    {
        if (lights == null || lights.Length == 0)
            CollectLights();

        foreach (var l in lights)
        {
            // 베이크 전용 라이트 제외하고 싶으면 조건 추가 가능
            if (l.lightmapBakeType == LightmapBakeType.Baked)
                continue;

            l.enabled = on;
        }
    }
}
