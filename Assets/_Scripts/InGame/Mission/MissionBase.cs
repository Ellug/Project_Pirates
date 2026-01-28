using UnityEngine;

public abstract class MissionBase : MonoBehaviour
{
    [Header("Mission Info")]
    [SerializeField] private string _missionTitle;
    [SerializeField] private string _missionDescription;
    [SerializeField] private float _missionScore;

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


        if (globalProgress != null)
            globalProgress.CompleteMission(_missionScore);

        OnMissionCompleted();
    }

    protected void FailMission()
    {
        if (isFinished)
            return;

        isFinished = true;


        OnMissionFailed();
    }

    protected virtual void OnMissionCompleted() { }
    protected virtual void OnMissionFailed() { }
}
