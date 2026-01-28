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
    [SerializeField] private GameObject[] _missionPrefabList;

    private void Awake()
    {
        Instance = this;
    }

    public void StartMission(int index)
    {
        if (index < _missionPrefabList.Length) 
        {
            while (_missionArea.childCount != 0)
                Destroy(_missionArea.GetChild(0).gameObject);

            MissionBase target = _missionPrefabList[index].GetComponent<MissionBase>();
            _title.text = target._missionTitle;
            _description.text = target._missionDescription;
            _missionPanel.SetActive(true);
            Instantiate(_missionPrefabList[index], _missionArea);
        }
        else
        {
            Debug.LogError("미션 컨테이너 인덱스 범위 벗어남.");
        }
    }
}
