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
    public event Action allReadyComplete; 

    // 아래 필드들은 마스터 클라이언트만 쓴다.
    private Dictionary<int, PlayerController> _playersId = new Dictionary<int, PlayerController>();
    private Transform[] _spawnPointList;
    private int _mafiaNum = 0;
    private int _citizenNum = 0;
    public int onLoadedPlayer = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _view = GetComponent<PhotonView>();
        }
        else if (Instance != this)
        {
            // 중복 인스턴스는 즉시 파괴
            Destroy(gameObject);
        }
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
        //PhotonNetwork.LoadLevel("InGame");

        // 인게임 씬에서 모두 왔는지 다시 확인한다.
        // 플레이어 프리팹이 모두 존재하는 것을 보장받기 위해
        yield return new WaitUntil(() => onLoadedPlayer >= playerNumber);

        // 씬의 모든 플레이어 찾아서 가져온 후 리스트에 담음
        // (지연으로 인한 누락 방지를 위해 플레이어 수와 같을 때까지 보장 받음)
        PlayerController[] players = null;
        yield return new WaitUntil(() =>
        {
            players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            return players.Length == playerNumber;
        });
        Debug.Log($"총 플레이어 수 : {players.Length}");

        _playersId.Clear();
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

        yield return null; // 혹시 모르니 인게임 씬 유니티 라이프 싸이클 한번 돌릴 시간

        // ============ 투표 시스템 초기화 ============
        if (VoteRoomProperties.Instance != null)
            VoteRoomProperties.Instance.InitializePlayerList();

        // ============ 무작위 스폰 포인트 선정 & 플레이어 이동 ============

        if (_spawnPointList != null)
        {
            int[] playerSpawnPosList = new int[players.Length];
            for (int i = 0; i < playerSpawnPosList.Length; )
            {
                int randSpawn = UnityEngine.Random.Range(1, _spawnPointList.Length);
                for (int j = 0; j < playerSpawnPosList.Length; j++)
                {
                    if (playerSpawnPosList[j] == 0)
                    {
                        playerSpawnPosList[j] = randSpawn;
                        i++;
                        break;
                    }
                    else if (playerSpawnPosList[j] == randSpawn)
                    {
                        break;
                    }
                }
            }
            // 검증: 딕셔너리의 플레이어 수와 스폰 포지션 리스트 길이는 같아야한다.
            if (_playersId.Count == playerSpawnPosList.Length)
            {
                int count = 0;
                foreach (var player in _playersId)
                {
                    player.Value.RequestSpawnPostion(_spawnPointList[playerSpawnPosList[count]].position);
                    count++;
                    if (count >= _spawnPointList.Length) break; // 이거 통과할 일 없겠지만 혹시 몰라 또 안전코드
                }
            }
        }

        // ============ 마피아 무작위로 부여 ============
        int firstEnemy = -1;
        int secondEnemy = -1;
        firstEnemy = UnityEngine.Random.Range(0, players.Length);
        _mafiaNum++; // 마피아 수 기억

        if (players.Length > 7) // 플레이어 수 7 초과면 마피아 1명 더 선정
        {
            _mafiaNum++;
            do // 중복 선정되지 않도록 do while 문 사용
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

        // ============ 직업도 무작위로 부여 ============
        List<JobId> jobDeck = new List<JobId>();

        // 플레이어 수가 5 미만이면 1명에게만 부여
        // 이건 테스트용으로 실제 게임에선 최소 5명의 플레이어가 요구됨
        if (players.Length < 5)
            jobDeck.Add(JobId.Doctor); // 테스트할 땐 여기 직업 바꿈
        else // 5명 이상 이면 직업들 여기 넣음
        {
            jobDeck.Add(JobId.Doctor);
            jobDeck.Add(JobId.Sprinter);
        }
        // 나머지는 무직으로 채움
        while (jobDeck.Count < players.Length)
            jobDeck.Add(JobId.None);

        // 리스트 섞기 (피셔-예이츠 셔플)
        for (int i = 0; i < jobDeck.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, jobDeck.Count);
            JobId temp = jobDeck[i];
            jobDeck[i] = jobDeck[rnd];
            jobDeck[rnd] = temp;
        }

        for (int i = 0; i < players.Length; i++)
            players[i].photonView.RPC("AssignJob", players[i].photonView.Owner, (int)jobDeck[i]);
        
        yield return null;

        // ============ 게임 시작을 모두에게 선언! ============
        _view.RPC(nameof(ChangeInGameScene), RpcTarget.All);
    }

    public void SetSpawnPointList(Transform[] spawnPointList)
    {
        _spawnPointList = spawnPointList;
    }

    public void NoticeDeathPlayer(PlayerController player)
    {
        // 마스터 클라이언트에게 내 죽음을 알림.
        _view.RPC(nameof(PlayerDeathCheck), RpcTarget.MasterClient, 
            player.GetComponent<PhotonView>().ViewID);
    }

    // 시체 생성 요청 -> 모든 클라이언트에서 로컬 생성
    private int _deadBodyIdCounter = 10000; // 시체 고유 ID 카운터

    public void RequestSpawnDeadBody(Vector3 position, Quaternion rotation)
    {
        int deadBodyId = _deadBodyIdCounter++;
        _view.RPC(nameof(RpcSpawnDeadBody), RpcTarget.All, position, rotation, deadBodyId);
    }

    [PunRPC]
    private void RpcSpawnDeadBody(Vector3 position, Quaternion rotation, int deadBodyId)
    {
        GameObject deadBodyPrefab = Resources.Load<GameObject>("DeadBodyMale");
        position.y += 0.4f;
        if (deadBodyPrefab != null)
        {
            GameObject deadBody = Instantiate(deadBodyPrefab, position, rotation);
            DeadBody db = deadBody.GetComponent<DeadBody>();
            if (db != null)
                db.InitializeWithId(deadBodyId);

            Debug.Log($"[DeadBody] 시체 생성 완료 - ID: {deadBodyId}, 위치: {position}");
        }
        else
        {
            Debug.LogError("[DeadBody] Deadbody 프리팹을 Resources 폴더에서 찾을 수 없습니다.");
        }
    }

    // 모든 시체 제거 요청 -> 모든 클라이언트에서 제거
    public void RequestRemoveAllDeadBodies()
    {
        _view.RPC(nameof(RpcRemoveAllDeadBodies), RpcTarget.All);
    }

    [PunRPC]
    private void RpcRemoveAllDeadBodies()
    {
        if (InteractionObjectRpcManager.Instance != null)
        {
            int removedCount = InteractionObjectRpcManager.Instance.UnregisterAndDestroyAllDeadBodies();
            Debug.Log($"[DeadBody] 모든 시체 제거 완료 - 총 {removedCount}개");
        }
    }

    public void NoticeGameOverToAllPlayers(bool isCitizenVictory)
    {
        _view.RPC(nameof(GameOverAndJudge), RpcTarget.All, isCitizenVictory);
    }

    // 시체 신고 완료 시 모든 플레이어 텔레포트 요청
    public void RequestTeleportAllPlayers(int reporterActorNumber)
    {
        _view.RPC(nameof(RpcTeleportAllPlayers), RpcTarget.All, reporterActorNumber);
    }

    [PunRPC]
    private void RpcTeleportAllPlayers(int reporterActorNumber)
    {
        if (VoteManager.Instance != null)
            VoteManager.Instance.OnDeadBodyReported(reporterActorNumber);
        else
            Debug.LogWarning("[PlayerManager] VoteManager.Instance가 null 임");
    }

    // 중앙 소집 버튼 사용 시 모든 플레이어 텔레포트 요청
    public void RequestCenterCall(int reporterActorNumber)
    {
        _view.RPC(nameof(RpcCenterCall), RpcTarget.All, reporterActorNumber);
    }

    [PunRPC]
    private void RpcCenterCall(int reporterActorNumber)
    {
        if (VoteManager.Instance != null)
            VoteManager.Instance.OnCenterReported(reporterActorNumber);
        else
            Debug.LogWarning("[PlayerManager] VoteManager.Instance가 null 임");
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

        // VoteRoomProperties에 사망 등록 (투표 UI에 반영)
        if (_playersId.TryGetValue(viewId, out var player))
        {
            int actorNumber = player.photonView.OwnerActorNr;
            if (VoteRoomProperties.Instance != null)
                VoteRoomProperties.Instance.MarkPlayerDead(actorNumber);
        }

        // 마피아 승리
        if (_citizenNum <= 0)
            NoticeGameOverToAllPlayers(false);
    }

    [PunRPC]
    public void GameOverAndJudge(bool isCitizenVictory)
    {
        // 이건 게임 종료시 모든 플레이어들이 RPC를 받아 실행하게 될 메서드임
        // 각 플레이어들은 자신의 세력을 보고 승리 또는 패배 패널 중 하나를 띄움.
        if (_localPlayer != null)
        {
            bool isWin;
            
            if (isCitizenVictory != _localPlayer.isMafia)
                isWin = true;
            else
                isWin = false;
            
            GameManager.Instance.EndGame(isWin);
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
