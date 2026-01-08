using UnityEngine;

public enum SceneState
{
    Title,
    Lobby,
    Room,
    InGame
}

public class GameManager : Singleton<GameManager>
{
    [Header("Global State")]
    [SerializeField] private string _nickname = "Developer";
    [SerializeField] private SceneState _flowState = SceneState.Title;

    public string Nickname => _nickname;
    public SceneState FlowState => _flowState;

    public PhotonPunManager Pun { get; private set; }

    private GameResultController _controller;

    protected override void OnSingletonAwake()
    {
        Pun = GetComponent<PhotonPunManager>();
    }

    public void SetNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return;

        _nickname = nickname;
    }

    public void SetSceneState(SceneState state)
    {
        _flowState = state;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    // 이 아래로 게임 결과 관련 메서드들
    // TODO : 시민과 해적의 승리로 나뉘므로 RPC로 뿌릴 때 자신의 역할에 따라 다른 결과가 나와야함.
    // 위 부분을 아직 고려 안한 상태
    public void RegistResultPanel(GameResultController controller)
    {
        _controller = controller;
    }

    public void GameOverAndResult(bool isCitizenVictory)
    {
        if (isCitizenVictory)
            CitizenVictory();
        else
            PiratesVictory();
    }

    // 시민의 승리
    public void CitizenVictory()
    {
        _controller.Victory();
        PauseGame();
    }

    // 해적의 승리
    public void PiratesVictory()
    {
        _controller.Defeat();
        PauseGame();
    }
}
