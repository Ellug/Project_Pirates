using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PostProcessingController : MonoBehaviour
{
    public static PostProcessingController Instance { get; private set; }

    [SerializeField] private Volume volume;
    [SerializeField] private float hitIntensity = 0.4f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private Vignette vignette;
    private Coroutine hitRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogError("Volume Profile에 Vignette가 없음");
        }
    }


    [ContextMenu("Test Hit Effect")]
    public void HitEffect()
    {
        if (vignette == null) return;

        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(TakeDamageFlash());
    }

    IEnumerator TakeDamageFlash()
    {
        vignette.active = true;
        vignette.intensity.value = hitIntensity;

        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            vignette.intensity.value = Mathf.Lerp(hitIntensity, 0f, t / fadeOutTime);
            yield return null;
        }

        vignette.intensity.value = 0f;
        vignette.active = false;
        hitRoutine = null;
    }
}
