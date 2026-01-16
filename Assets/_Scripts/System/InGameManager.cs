using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [Header("Create Voice Prefab")]
    [SerializeField] private CreateVoice _createVoice;

    [Header("PopUp Ui")]
    [SerializeField] private Image _playerRoleUi;

    private bool _ended;
    private PlayerContoller _player;

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SpawnPlayer());        
    }

    private void Start()
    {
        if (PlayerManager.Instance != null) 
            PlayerManager.Instance.allReadyComplete += PopUpPlayersRole;
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete -= PopUpPlayersRole;
    }

    IEnumerator SpawnPlayer()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (PlayerContoller.LocalInstancePlayer == null)
            PlayerContoller.LocalInstancePlayer = 
                PhotonNetwork.Instantiate("PlayerMale", new Vector3(0f, 3f, 0f), Quaternion.identity);
        
        PhotonView myPV = PlayerContoller.LocalInstancePlayer.GetComponent<PhotonView>();
        _createVoice.CreateVoicePV(myPV, PlayerContoller.LocalInstancePlayer.transform);
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
        roleText.text = "당신의 직업은 \"시민\" 입니다.";
        roleText.color = new Color(1f, 1f, 1f, 0f);
        _playerRoleUi.DOFade(1f, duration);
        roleText.DOFade(1f, duration);
        yield return new WaitForSeconds(duration * 3f);
        _playerRoleUi.DOFade(0f, duration);
        roleText.DOFade(0f, duration);
        yield return new WaitForSeconds(duration);
    }

    public void RegistPlayer(PlayerContoller player)
    {
        _player = player;
    }

    // 이건 인게임에서 esc키로 패널 띄우고 게임 나가는거 구현되면 지우기
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 0 키 → 전원 나가기 (마스터만)
        if (kb.digit0Key.wasPressedThisFrame)
            EndGameForAll();

        // 9 키 → 나만 나가기
        if (kb.digit9Key.wasPressedThisFrame)
            ExitForLocal();
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

        if (PlayerContoller.LocalInstancePlayer != null)
            PhotonNetwork.Destroy(PlayerContoller.LocalInstancePlayer);

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
        if (PlayerContoller.LocalInstancePlayer != null)
            PhotonNetwork.Destroy(PlayerContoller.LocalInstancePlayer);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Room");
    }
}
