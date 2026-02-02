using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 글로벌 진행도의 역할
// 1. 미션 클리어 소식을 받으면 해당 점수만큼 진행도를 증가 시킴.
// 2. 일정 시간마다 진행도를 1%씩 증가 시킴.
// 3. 진행도가 100%가 되면 시민 승리
public class GlobalProgress : MonoBehaviourPunCallbacks
{
    [Header("Reference")]
    [SerializeField] private Image _progressBar;
    [SerializeField] private TextMeshProUGUI _progressPercent;

    private ExitGames.Client.Photon.Hashtable _roomProps;
    private string _roomPropKey = "Progress";

    private IEnumerator Start()
    {
        if (PhotonNetwork.CurrentRoom == null)
            yield break;

        _roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

        // 마스터 클라이언트가 대표로 초기화
        if (PhotonNetwork.IsMasterClient)
        {
            _roomProps[_roomPropKey] = 0f;

            yield return new WaitForSeconds(2f);

            PhotonNetwork.CurrentRoom.SetCustomProperties(_roomProps);
            StartCoroutine(AutoProgress());
        }
    }

    // 코루틴으로 30초에 1퍼센트씩 증가
    // (이건 마스터만 돌려야함. 모두가 돌리면 30초마다 사람 수만큼 퍼센트가 증가할 것임.)
    private IEnumerator AutoProgress()
    {
        WaitForSeconds interval = new WaitForSeconds(30f);

        while (true)
        {
            yield return interval;
            IncreaseProgress(1f);
        }
    }

    // 외부에서 호출하며 미션 클리어시 점수를 받음.
    public void CompleteMission(float missionScore)
    {
        IncreaseProgress(missionScore);
    }

    // 진행도를 증가 시킴.
    // 룸 커스텀 값을 변경 시키므로 자동으로 맨 아래 콜백 함수가 수행됨.
    private void IncreaseProgress(float amount)
    {
        if (_roomProps.ContainsKey(_roomPropKey))
        {
            float temp = (float)_roomProps[_roomPropKey];
            temp += amount;
            _roomProps[_roomPropKey] = temp;
            PhotonNetwork.CurrentRoom.SetCustomProperties(_roomProps);
        }
    }

    // 콜백을 받으면 UI에 그 값을 적용 시킴.
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(_roomPropKey))
        {
            float progress = (float)changedProps[_roomPropKey];
            if (progress > 100f)  
                progress = 100f;

            _progressPercent.text = $"{progress:f1} %";
            _progressBar.fillAmount = progress / 100f;

            //  100퍼센트 달성 시 마스터 클라이언트가 대표로 승리 선언
            if (PhotonNetwork.IsMasterClient && progress >= 100f)
            {
                PlayerManager.Instance.NoticeGameOverToAllPlayers(true);
            }
        }
    }
}
