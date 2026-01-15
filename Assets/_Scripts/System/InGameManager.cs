using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [Header("Create Voice Prefab")]
    [SerializeField] private CreateVoice _createVoice;

    private bool _ended;

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SpawnPlayer());        
    }

    IEnumerator SpawnPlayer()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (PlayerContoller.LocalInstancePlayer == null)
            PlayerContoller.LocalInstancePlayer = 
                PhotonNetwork.Instantiate("PlayerMale", new Vector3(0f, 3f, 0f), Quaternion.identity);
        _createVoice.CreateVoicePV(PhotonNetwork.LocalPlayer, PlayerContoller.LocalInstancePlayer.transform);
    }

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
