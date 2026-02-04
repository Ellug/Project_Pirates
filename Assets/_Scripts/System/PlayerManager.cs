using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private Coroutine _gameInitCoroutine;

    private const string SCENE_INGAMELOADING = "InGameLoading";
    private const string LOADED_KEY = "OnLoaded";
    private const string ROLE_MAFIA_KEY = "RoleMafia";
    private const string DEAD_KEY = "IsDead";

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
    public void StartGameInit(int playerNumber, int initialLoaded = 0)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (_gameInitCoroutine != null) return;

        onLoadedPlayer = Mathf.Max(0, initialLoaded);
        _gameInitCoroutine = StartCoroutine(GameInitLogic(playerNumber));
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
            _gameInitCoroutine = null;
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

        if (players.Length > 7) // 플레이어 수 7 초과면 마피아 1명 더 선정
        {
            do // 중복 선정되지 않도록 do while 문 사용
            {
                secondEnemy = UnityEngine.Random.Range(0, players.Length);
            } while (firstEnemy == secondEnemy);

        }

        _mafiaNum = secondEnemy != -1 ? 2 : 1;
        _citizenNum = players.Length - _mafiaNum;

        // 역할/사망 상태 초기화 (안정적인 카운트용)
        foreach (var p in players)
        {
            var owner = p.photonView != null ? p.photonView.Owner : null;
            if (owner == null) continue;

            var baseProps = new Hashtable
            {
                { ROLE_MAFIA_KEY, false },
                { DEAD_KEY, false }
            };
            owner.SetCustomProperties(baseProps);
        }

        // 마피아 역할 지정 (Custom Properties)
        SetPlayerRole(players[firstEnemy]?.photonView?.Owner, true);
        if (secondEnemy != -1)
            SetPlayerRole(players[secondEnemy]?.photonView?.Owner, true);

        // 마피아를 알림
        players[firstEnemy].photonView.RPC("IsMafia", players[firstEnemy].photonView.Owner);
        if (secondEnemy != -1)
            players[secondEnemy].photonView.RPC("IsMafia", players[secondEnemy].photonView.Owner);
        yield return null;

        // ============ 직업도 무작위로 부여 ============
        List<JobId> jobDeck = new List<JobId>();
        List<int> alreadyUsed = new List<int>();

        // 구현된 직업들 중 무작위로 5개를 뽑아 직업 배분 예정. 나머진 무직
        while (jobDeck.Count < 5)
        {
            int randJob = UnityEngine.Random.Range(1, (int)JobId.End);
            if (alreadyUsed.Contains(randJob) == false)
            {
                jobDeck.Add((JobId)randJob);
                alreadyUsed.Add(randJob);
            }
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

        _gameInitCoroutine = null;
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
        if (!_playersId.TryGetValue(viewId, out var target))
        {
            var pv = PhotonView.Find(viewId);
            if (pv != null)
                target = pv.GetComponent<PlayerController>();
            if (target != null)
                _playersId[viewId] = target;
        }

        if (target == null)
        {
            Debug.LogWarning($"[PlayerManager] PlayerDeathCheck 실패: viewId={viewId}");
            return;
        }

        var owner = target.photonView != null ? target.photonView.Owner : null;
        if (owner != null && IsPlayerDead(owner))
        {
            // 투표 등으로 이미 사망 처리된 경우에도 승패 체크는 필요함
            EvaluateWinConditions();
            return;
        }

        // VoteRoomProperties에 사망 등록 (투표 UI에 반영)
        int actorNumber = target.photonView.OwnerActorNr;
        if (VoteRoomProperties.Instance != null)
            VoteRoomProperties.Instance.MarkPlayerDead(actorNumber);

        // Custom Properties에 사망 저장 (카운트 안정화)
        if (owner != null)
            SetPlayerDead(owner, true);

        EvaluateWinConditions();
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

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        RebuildPlayerCache();

        // 로딩 중 마스터가 바뀌면 새 마스터가 초기화 로직을 이어서 수행
        string scene = SceneManager.GetActiveScene().name;
        if (scene == SCENE_INGAMELOADING)
        {
            int count = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
            int loaded = CountLoadedPlayers();
            StartGameInit(count, loaded);
        }

        // 마스터 전환 직후 승패 체크
        EvaluateWinConditions();
    }

    private void RebuildPlayerCache()
    {
        _playersId.Clear();

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            var pv = p.GetComponent<PhotonView>();
            if (pv == null) continue;
            _playersId[pv.ViewID] = p;
        }
    }

    private static int CountLoadedPlayers()
    {
        int loaded = 0;
        var list = PhotonNetwork.PlayerList;
        if (list == null) return loaded;

        for (int i = 0; i < list.Length; i++)
        {
            var p = list[i];
            if (p != null && p.CustomProperties != null &&
                p.CustomProperties.TryGetValue(LOADED_KEY, out var v) &&
                v is bool b && b)
            {
                loaded++;
            }
        }

        return loaded;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 인게임 중 이탈자는 사망 처리로 간주
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "InGame") return;

        if (otherPlayer != null && VoteRoomProperties.Instance != null)
            VoteRoomProperties.Instance.MarkPlayerDead(otherPlayer.ActorNumber);

        EvaluateWinConditions();
    }

    private void SetPlayerRole(Player player, bool isMafia)
    {
        if (player == null) return;
        var props = new Hashtable { { ROLE_MAFIA_KEY, isMafia } };
        player.SetCustomProperties(props);
    }

    private void SetPlayerDead(Player player, bool isDead)
    {
        if (player == null) return;
        var props = new Hashtable { { DEAD_KEY, isDead } };
        player.SetCustomProperties(props);
    }

    private bool IsPlayerDead(Player player)
    {
        if (player == null) return false;
        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(DEAD_KEY, out var v) &&
            v is bool b)
            return b;

        return VoteRoomProperties.Instance != null &&
               VoteRoomProperties.Instance.IsPlayerDead(player.ActorNumber);
    }

    private bool IsPlayerMafia(Player player)
    {
        if (player == null) return false;
        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(ROLE_MAFIA_KEY, out var v) &&
            v is bool b)
            return b;
        return false;
    }

    private void EvaluateWinConditions()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null) return;
        if (SceneManager.GetActiveScene().name != "InGame") return;

        int aliveMafia = 0;
        int aliveCitizen = 0;
        bool hasRoleInfo = false;

        var list = PhotonNetwork.PlayerList;
        for (int i = 0; i < list.Length; i++)
        {
            var p = list[i];
            if (p == null) continue;
            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(ROLE_MAFIA_KEY))
                hasRoleInfo = true;
            if (IsPlayerDead(p)) continue;

            if (IsPlayerMafia(p))
                aliveMafia++;
            else
                aliveCitizen++;
        }

        if (!hasRoleInfo)
            return;

        _mafiaNum = aliveMafia;
        _citizenNum = aliveCitizen;

        if (aliveMafia <= 0 && aliveCitizen > 0)
            NoticeGameOverToAllPlayers(true);  // 시민 승리
        else if (aliveCitizen <= 0 && aliveMafia > 0)
            NoticeGameOverToAllPlayers(false); // 마피아 승리
    }

    // Room 씬으로 돌아갈 때 인게임 상태 초기화
    public void ResetForRoom()
    {
        // 실행 중인 코루틴 중지
        if (_gameInitCoroutine != null)
        {
            StopCoroutine(_gameInitCoroutine);
            _gameInitCoroutine = null;
        }

        // 플레이어 캐시 초기화
        _playersId.Clear();
        _localPlayer = null;

        // 게임 상태 초기화
        _mafiaNum = 0;
        _citizenNum = 0;
        onLoadedPlayer = 0;
        _spawnPointList = null;

        Debug.Log("[PlayerManager] Room 복귀를 위한 상태 초기화 완료");
    }
}


