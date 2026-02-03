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
            // 매 게임 시작 시 색상 중복 방지 위해 강제 재할당
            SetPlayerColor.AssignColorsToAll(true);
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

    // 전원 로비로 이동
    public static void ExitForLocal()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[InGame] ExitForLocal ignored: not master");
            return;
        }

        Debug.Log("[InGame] ExitForLocal -> LoadLevel(Room)");

        // 모든 플레이어의 네트워크 오브젝트 파괴 (버퍼 정리)
        // LoadLevel 전에 파괴해야 나중에 입장하는 플레이어에게 이전 오브젝트가 생성되지 않음
        foreach (var player in PhotonNetwork.PlayerList)
        {
            PhotonNetwork.DestroyPlayerObjects(player);
        }

        PlayerController.LocalInstancePlayer = null;

        // LoadLevel로 모든 클라이언트가 동기화되어 Room 씬으로 이동
        PhotonNetwork.LoadLevel("Room");
    }
}
