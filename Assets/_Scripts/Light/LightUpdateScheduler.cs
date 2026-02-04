using System.Collections.Generic;
using UnityEngine;

public class LightUpdateScheduler : MonoBehaviour
{
    public static LightUpdateScheduler Instance { get; private set; }

    [Header("Batch Settings")]
    [SerializeField] private int lightsPerFrame = 20;

    private readonly List<ProximityLight> lightList = new List<ProximityLight>();
    private int cursor;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(ProximityLight light)
    {
        if (!lightList.Contains(light))
            lightList.Add(light);
    }

    public void Unregister(ProximityLight light)
    {
        lightList.Remove(light);
    }

    public List<ProximityLight> GetNextBatch()
    {
        var result = new List<ProximityLight>();

        if (lightList.Count == 0)
            return result;

        int count = Mathf.Min(lightsPerFrame, lightList.Count);

        for (int i = 0; i < count; i++)
        {
            if (cursor >= lightList.Count)
                cursor = 0;

            result.Add(lightList[cursor]);
            cursor++;
        }

        return result;
    }
}
