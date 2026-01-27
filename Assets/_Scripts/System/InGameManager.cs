using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _spawnPointParent;
    
    [Header("Create Voice Prefab")]
    [SerializeField] private CreateVoice _createVoice;

    [Header("PopUp Ui")]
    [SerializeField] private Image _playerRoleUi;

    private bool _ended;
    private PlayerController _player;

    private const string UPPER_COLOR_KEY = "UpperColor"; // [ADD] 상의 색상 키

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SpawnPlayer());
    }

    private void Start()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete += PopUpPlayersRole;

        GameManager.Instance.SetSceneState(SceneState.InGame);
        PlayerManager.Instance.SetSpawnPointList(
            _spawnPointParent.transform.GetComponentsInChildren<Transform>()
            );
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete -= PopUpPlayersRole;
    }

    IEnumerator SpawnPlayer()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        yield return new WaitForSeconds(3f);

        if (PlayerController.LocalInstancePlayer == null)
        {
            // 테스트용 임시 코드
            int type = Random.Range(0, 2);
            string char_type = type == 0 ? "PlayerFemale" : "PlayerMale";

            PlayerController.LocalInstancePlayer =
                PhotonNetwork.Instantiate(char_type,
                    new Vector3(0f, 3f, 0f),
                    Quaternion.identity);
        }

        PhotonView myPV = PlayerController.LocalInstancePlayer.GetComponent<PhotonView>();

        yield return new WaitUntil(() => myPV.ViewID > 0);

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(
            _createVoice.CreateVoicePV(myPV, PlayerController.LocalInstancePlayer.transform));

        // ✅ 스폰이 끝난 뒤, 마스터가 색 배정
        if (PhotonNetwork.IsMasterClient)
        {
            SetPlayerColor.AssignColorsToAll();
        }
    }

    public void PopUpPlayersRole()
    {
        StartCoroutine(StartGuide());
    }

    IEnumerator StartGuide()
    {
        yield return new WaitUntil(() => _player != null);

        TextMeshProUGUI roleText = _playerRoleUi.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        float duration = 0.8f;

        yield return StartCoroutine(PopUpRole(roleText, duration));

        yield return StartCoroutine(PopUpJob(roleText, duration));

        _playerRoleUi.gameObject.SetActive(false);
    }

    IEnumerator PopUpRole(TextMeshProUGUI roleText, float duration)
    {
        if (_player.isMafia)
        {
            roleText.text = "당신은 \"마피아\" 입니다.";
            roleText.color = new Color(1f, 0.25f, 0.25f, 0f);
        }
        _playerRoleUi.DOFade(1f, duration);
        roleText.DOFade(1f, duration);
        yield return new WaitForSeconds(duration * 3f);
        _playerRoleUi.DOFade(0f, duration);
        roleText.DOFade(0f, duration);
        yield return new WaitForSeconds(duration);
    }

    IEnumerator PopUpJob(TextMeshProUGUI roleText, float duration)
    {
        BaseJob jobType = _player.GetPlayerJob();
        string jobName = "평범한 시민";

        if (jobType != null) // null 이 아니면 직업이 있고 그 이름을 가져옴
            jobName = jobType.name;

        roleText.text = $"당신의 직업은 \"{jobName}\" 입니다.";
        roleText.color = new Color(1f, 1f, 1f, 0f);
        _playerRoleUi.DOFade(1f, duration);
        roleText.DOFade(1f, duration);
        yield return new WaitForSeconds(duration * 3f);
        _playerRoleUi.DOFade(0f, duration);
        roleText.DOFade(0f, duration);
        yield return new WaitForSeconds(duration);
    }

    public void RegistPlayer(PlayerController player)
    {
        _player = player;
    }

    // Todo : 종료 후에 방으로 가는데, 이거 일반적인 게임 플로우처럼 처리해야함.
    // 전원 종료
    private void EndGameForAll()
    {
        if (_ended) return;

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[InGame] EndGameForAll ignored: not master");
            return;
        }

        _ended = true;

        if (PlayerController.LocalInstancePlayer != null)
            PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);

        Debug.Log("[InGame] EndGameForAll -> LoadLevel(Room)");
        PhotonNetwork.LoadLevel("Room");
    }

    // 나만 종료
    public void ExitForLocal()
    {
        Debug.Log("[InGame] ExitForLocal -> LoadScene(Room)");

        GameManager.Instance.ResumeGame();

        // Photon 룸은 유지한 채, 로컬 씬만 이동
        // PN 뭔가 해줘야한다 -> 플레이어를 명시적으로 파괴해야함.
        if (PlayerController.LocalInstancePlayer != null)
            PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Room");
    }
}
