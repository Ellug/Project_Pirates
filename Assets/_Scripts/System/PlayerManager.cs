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
    // 결과를 나머지에게 알린다.
    public void StartGameInit(int playerNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        StartCoroutine(GameInitLogic(playerNumber));

        return;
        // 씬의 모든 플레이어 찾아서 가져온 후 리스트에 담음
        _players = FindObjectsByType<PlayerContoller>(FindObjectsSortMode.None).ToList();

        // 플레이어를 못 가져오는 등 뭔가 문제 발생 시 방어
        if (_players.Count == 0)
        {
            Debug.LogError("Can't found players");
            return;
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
        // 마피아를 알림.
        _view.RPC("IsMafia", _players[firstEnemy].photonView.Owner);
        if (secondEnemy != -1)
            _view.RPC("IsMafia", _players[secondEnemy].photonView.Owner);
    }

    private IEnumerator GameInitLogic(int playerNumber)
    {
        Debug.Log($"{playerNumber}");
        // 모든 플레이어가 로딩될 때까지 기다린다.
        yield return new WaitUntil(() => onLoadedPlayer >= playerNumber);

        // 모든 플레이어가 로딩이 되면 인게임 씬으로 전환한다.
        _view.RPC("ChangeInGameScene", RpcTarget.All);
    }

    [PunRPC]
    public void ChangeInGameScene()
    {
        allReadyComplete.Invoke();
    }

    public void GameOver()
    {
        Destroy(gameObject);
    }
}
