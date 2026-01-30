using TMPro;
using UnityEngine;

public class MissionContainer : MonoBehaviour
{
    public static MissionContainer Instance;

    [SerializeField] private GlobalProgress _progressUi;
    [SerializeField] private GameObject _missionPanel;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Transform _missionArea;
    public Material clearMaterial;

    [SerializeField] private GameObject[] _missionPrefabList;

    private MissionInteraction _missionObj;
    private GameObject _missionInstance;

    private void Awake()
    {
        Instance = this;
    }

    public void StartMission(int index, MissionInteraction missionObj)
    {
        if (index < _missionPrefabList.Length) 
        {
            _missionObj = missionObj;

            _missionInstance = Instantiate(_missionPrefabList[index], _missionArea);
            MissionBase target = _missionInstance.GetComponent<MissionBase>();
            _title.text = target._missionTitle;
            _description.text = target._missionDescription;
            target.Init();
            _missionPanel.SetActive(true);
            InputManager.Instance.SetUIMode(true);
        }
        else
        {
            Debug.LogError("미션 컨테이너 인덱스 범위 벗어남.");
        }
    }

    public void OnClickExitButton()
    {
        CloseMissionPanel();
    }

    public void CloseMissionPanel()
    {
        if (_missionInstance != null)
            Destroy(_missionInstance);

        _missionObj.ExitUse();
        _missionPanel.SetActive(false);
        InputManager.Instance.SetUIMode(false);
    }

    public void ClearMission(float score)
    {
        _progressUi.CompleteMission(score);
        _missionObj.MissionCleared();
        CloseMissionPanel();
    }
}
