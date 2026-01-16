using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance { get; private set; }
    public event Action allReadyComplete; 

    private PhotonView _view;
    private List<PlayerContoller> _players = new List<PlayerContoller>();
    public int onLoadedPlayer = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        _view = GetComponent<PhotonView>();
    }

    // 게임 시작시 세팅은 마스터 클라이언트만 실행
    public void StartGameInit(int playerNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        StartCoroutine(GameInitLogic(playerNumber));
    }

    private IEnumerator GameInitLogic(int playerNumber)
    {
        Debug.Log($"{playerNumber}");
        // 모든 플레이어가 로딩될 때까지 기다린다.
        yield return new WaitUntil(() => onLoadedPlayer >= playerNumber);

        onLoadedPlayer = 0;
        // 모든 플레이어가 로딩이 되면 인게임 씬으로 전환한다.
        _view.RPC("ChangeInGameScene", RpcTarget.All);

        // 인게임 씬에서 모두 왔는지 다시 확인한다.
        // 플레이어 프리팹이 모두 존재하는 것을 보장받기 위해
        yield return new WaitUntil(() => onLoadedPlayer >= playerNumber);

        // 씬의 모든 플레이어 찾아서 가져온 후 리스트에 담음
        _players = FindObjectsByType<PlayerContoller>(FindObjectsSortMode.None).ToList();

        // 모든 플레이어를 못 가져오는 등 뭔가 문제 발생 시 방어
        if (_players.Count != PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.LogError("Can't found all players");
            yield break;
        }
        Debug.Log($"총 플레이어 수 : {_players.Count}");
        int firstEnemy = -1;
        int secondEnemy = -1;

        firstEnemy = UnityEngine.Random.Range(0, _players.Count);

        if (_players.Count > 7) // 플레이어 수 7 초과면 마피아 1명 더 선정
        {
            // 중복 선정되지 않도록 do while 문 사용
            do
            {
                secondEnemy = UnityEngine.Random.Range(0, _players.Count);
            } while (firstEnemy == secondEnemy);

        }
        // 마피아를 알림
        _players[firstEnemy].photonView.RPC("IsMafia", _players[firstEnemy].photonView.Owner);
        if (secondEnemy != -1)
            _players[secondEnemy].photonView.RPC("IsMafia", _players[secondEnemy].photonView.Owner);
        yield return null;

        // 직업도 무작위로 부여함 (아직 미구현)
        yield return null;

        // 게임 시작을 모두에게 선언!
        _view.RPC("ChangeInGameScene", RpcTarget.All);
    }

    [PunRPC]
    public void ChangeInGameScene()
    {
        allReadyComplete.Invoke();
    }

    [PunRPC]
    public void PlayerEnterCheck()
    {
        onLoadedPlayer++;
    }

    public void GameOver()
    {
        Destroy(gameObject);
    }
}
