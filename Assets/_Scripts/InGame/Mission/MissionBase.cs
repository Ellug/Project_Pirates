using UnityEngine;

public abstract class MissionBase : MonoBehaviour
{
    [Header("Mission Info")]
    public string _missionTitle;
    public string _missionDescription;
    [SerializeField] private float _missionScore;

    public virtual void Init() { }

    protected void CompleteMission()
    {
        MissionContainer.Instance.ClearMission(_missionScore);
    }
}
