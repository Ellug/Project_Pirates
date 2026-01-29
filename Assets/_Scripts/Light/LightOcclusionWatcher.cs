using UnityEngine;

public class LightOcclusionWatcher : MonoBehaviour
{
    [SerializeField] private ProximityLight target;

    private void OnBecameVisible()
    {
        if (target != null)
            target.SetByOcclusion(true);
    }

    private void OnBecameInvisible()
    {
        if (target != null)
            target.SetByOcclusion(false);
    }
}
