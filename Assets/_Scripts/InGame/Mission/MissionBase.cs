using UnityEngine;

public abstract class MissionBase : MonoBehaviour
{
    [Header("Mission Info")]
    [SerializeField] protected string missionId;
    [SerializeField] private string _missionTitle;
    [SerializeField] private string _missionDescription;

    protected GlobalProgress globalProgress;
    protected bool isFinished;

    protected virtual void Awake()
    {
        globalProgress = FindFirstObjectByType<GlobalProgress>();
    }

    protected void CompleteMission()
    {
        if (isFinished)
            return;

        isFinished = true;

        Debug.Log($"[Mission] Complete : {missionId}");

        if (globalProgress != null)
            globalProgress.CompleteMission(missionId);

        OnMissionCompleted();
    }

    protected void FailMission()
    {
        if (isFinished)
            return;

        isFinished = true;

        Debug.Log($"[Mission] Failed : {missionId}");

        OnMissionFailed();
    }

    protected virtual void OnMissionCompleted() { }
    protected virtual void OnMissionFailed() { }
}
