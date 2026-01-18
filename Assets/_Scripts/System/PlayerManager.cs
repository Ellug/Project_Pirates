using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance { get; private set; }
    private PlayerController _localPlayer;
    private PhotonView _view;

    // 아래 필드들은 마스터 클라이언트만 쓴다.
    private Dictionary<int, PlayerController> _playersId = new Dictionary<int, PlayerController>();
    private int _mafiaNum = 0;
    private int _citizenNum = 0;
    public int onLoadedPlayer = 0;
    public event Action allReadyComplete; 

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
        _view.RPC(nameof(ChangeInGameScene), RpcTarget.All);

        // 인게임 씬에서 모두 왔는지 다시 확인한다.
        // 플레이어 프리팹이 모두 존재하는 것을 보장받기 위해
        yield return new WaitUntil(() => onLoadedPlayer >= playerNumber);

        // 씬의 모든 플레이어 찾아서 가져온 후 리스트에 담음
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            _playersId[p.GetComponent<PhotonView>().ViewID] = p;
        }

        // 모든 플레이어를 못 가져오는 등 뭔가 문제 발생 시 방어
        if (players.Length != PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.LogError("Can't found all players");
            yield break;
        }
        Debug.Log($"총 플레이어 수 : {players.Length}");
        int firstEnemy = -1;
        int secondEnemy = -1;

        firstEnemy = UnityEngine.Random.Range(0, players.Length);
        // 마피아 수 기억
        _mafiaNum++;

        if (players.Length > 7) // 플레이어 수 7 초과면 마피아 1명 더 선정
        {
            _mafiaNum++;
            // 중복 선정되지 않도록 do while 문 사용
            do
            {
                secondEnemy = UnityEngine.Random.Range(0, players.Length);
            } while (firstEnemy == secondEnemy);

        }
        // 시민 수 기억
        _citizenNum = players.Length - _mafiaNum;

        // 마피아를 알림
        players[firstEnemy].photonView.RPC("IsMafia", players[firstEnemy].photonView.Owner);
        if (secondEnemy != -1)
            players[secondEnemy].photonView.RPC("IsMafia", players[secondEnemy].photonView.Owner);
        yield return null;

        // 직업도 무작위로 부여함 (아직 미구현)
        yield return null;

        // 게임 시작을 모두에게 선언!
        _view.RPC(nameof(ChangeInGameScene), RpcTarget.All);
    }

    public void NoticeDeathPlayer(PlayerController player)
    {
        // 마스터 클라이언트에게 내 죽음을 알림.
        _view.RPC(nameof(PlayerDeathCheck), RpcTarget.MasterClient, 
            player.GetComponent<PhotonView>().ViewID);
    }

    public void NoticeGameOverToAllPlayers(bool isCitizenVictory)
    {
        _view.RPC(nameof(GameOverAndJudge), RpcTarget.All, isCitizenVictory);
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

    [PunRPC]
    public void PlayerDeathCheck(int viewId)
    {
        // 죽은 놈을 찾아서 걔가 마피아인지 알아야하고 죽음 처리한다.
        if (_playersId[viewId].isMafia)
            _mafiaNum--;
        else
            _citizenNum--;

        // 마피아 승리
        if (_citizenNum == 0)
            NoticeGameOverToAllPlayers(false);  
    }

    [PunRPC]
    public void GameOverAndJudge(bool isCitizenVictory)
    {
        // 이건 게임 종료시 모든 플레이어들이 RPC를 받아 실행하게 될 메서드임
        // 각 플레이어들은 자신의 세력을 보고 승리 또는 패배 패널 중 하나를 띄움.
        if (_localPlayer != null)
        {
            if (isCitizenVictory != _localPlayer.isMafia)
                GameManager.Instance.Victory();
            else
                GameManager.Instance.Defeat();

        }
    }

    public void RegistLocalPlayer(PlayerController localPlayer)
    {
        _localPlayer = localPlayer;
    }

    public void GameOver()
    {
        Destroy(gameObject);
    }
}
