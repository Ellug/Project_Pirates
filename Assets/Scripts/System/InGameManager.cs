using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameManager : MonoBehaviourPunCallbacks
{
    private bool _ended;

    public override void OnEnable()
    {
        base.OnEnable();
        if (PlayerContoller.LocalInstancePlayer == null)
        {
            PhotonNetwork.Instantiate("Player", new Vector3(0f, 3f, 0f), Quaternion.identity);
        }
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

        Debug.Log("[InGame] EndGameForAll -> LoadLevel(Room)");
        PhotonNetwork.LoadLevel("Room");
    }

    // 나만 종료
    public void ExitForLocal()
    {
        Debug.Log("[InGame] ExitForLocal -> LoadScene(Room)");

        GameManager.Instance.ResumeGame();

        // Photon 룸은 유지한 채, 로컬 씬만 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("Room");
    }
}
