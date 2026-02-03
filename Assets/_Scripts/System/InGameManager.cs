using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _spawnPointParent;
    
    [Header("Create Voice Prefab")]
    [SerializeField] private CreateVoice _createVoice;

    [Header("PopUp Ui")]
    [SerializeField] private Image _playerRoleUi;

    [Header("Fade Ui")]
    [SerializeField] private FadeController _fadeController;

    private bool _ended;
    private PlayerController _player;

    private const string UPPER_COLOR_KEY = "UpperColor"; // [ADD] 상의 색상 키

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SpawnPlayer());
    }

    void Start()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete += PopUpPlayersRole;

        GameManager.Instance.SetSceneState(SceneState.InGame);
        PlayerManager.Instance.SetSpawnPointList(
            _spawnPointParent.transform.GetComponentsInChildren<Transform>()
            );
    }

    void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete -= PopUpPlayersRole;
    }

    IEnumerator SpawnPlayer()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        yield return new WaitForSeconds(3f);

        int maxPlayerCount = PhotonNetwork.CurrentRoom.MaxPlayers;
        int myPlayerNum = PhotonNetwork.LocalPlayer.ActorNumber;
        
        int SpawnPos = (myPlayerNum - 1) % maxPlayerCount;

        if (PlayerController.LocalInstancePlayer == null)
        {
            // 테스트용 임시 코드 (남여 랜덤 생성)
            //int type = Random.Range(0, 2);
            //string char_type = type == 0 ? "PlayerFemale" : "PlayerMale";

            PlayerController.LocalInstancePlayer =
                PhotonNetwork.Instantiate("PlayerMale",
                    new Vector3(3f, 1f, SpawnPos * 2),
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
        BaseJob jobType = _player.GetPlayerJob();

        _fadeController.StartInGameFade(_player.isMafia, jobType);
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
    public static void ExitForLocal()
    {
        Debug.Log("[InGame] ExitForLocal -> LoadScene(Room)");

        // Photon 룸은 유지한 채, 로컬 씬만 이동
        // PN 뭔가 해줘야한다 -> 플레이어를 명시적으로 파괴해야함.
        if (PlayerController.LocalInstancePlayer != null)
            PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Room");
    }
}
