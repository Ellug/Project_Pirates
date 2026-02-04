using System.Collections;
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
    private Transform _playerTransform;
    private Coroutine _distanceCheckCor;
    private float _validDistance = 3.2f;

    void Awake()
    {
        Instance = this;
    }

    public void StartMission(int index, MissionInteraction missionObj, Transform playerTransform)
    {
        if (index < _missionPrefabList.Length) 
        {
            _missionObj = missionObj;
            _playerTransform = playerTransform;

            _missionInstance = Instantiate(_missionPrefabList[index], _missionArea);
            MissionBase target = _missionInstance.GetComponent<MissionBase>();
            _title.text = target._missionTitle;
            _description.text = target._missionDescription;
            target.Init();
            _missionPanel.SetActive(true);
            InputManager.Instance.SetUIMode(true);
            _distanceCheckCor = StartCoroutine(CheckDistance());
        }
        else
        {
            Debug.LogError("미션 컨테이너 인덱스 범위 벗어남.");
        }
    }

    private IEnumerator CheckDistance()
    {
        if (_playerTransform == null || _missionObj == null)
            yield break;

        float dis;
        WaitForSeconds interval = new WaitForSeconds(0.2f);
        while (_missionPanel.gameObject.activeSelf)
        {
            dis = Vector3.Distance(_playerTransform.position, _missionObj.transform.position);
            if (dis > _validDistance)
            {
                CloseMissionPanel();
                yield break;
            }

            yield return interval;
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

        if (_distanceCheckCor != null)
            StopCoroutine(_distanceCheckCor);

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
