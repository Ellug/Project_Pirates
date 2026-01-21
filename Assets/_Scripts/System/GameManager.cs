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
    [SerializeField] private SceneState _flowState = SceneState.Title;

    public SceneState FlowState => _flowState;

    public PhotonPunManager Pun { get; private set; }

    private GameResultController _controller;

    protected override void OnSingletonAwake()
    {
        Pun = GetComponent<PhotonPunManager>();
    }

    public void SetSceneState(SceneState state)
    {
        _flowState = state;

        if (AudioManager.Instance != null)
        AudioManager.Instance.PlayBgm(state);
    }


    // 네트워크 게임이기에 Time.timeScale로 퍼즈하는 것 보단 게임 종료 연출을 따로 만드는 게 좋을 거 같음
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

    public void Victory()
    {
        _controller.Victory();
        GameOver();
    }

    public void Defeat()
    {
        _controller.Defeat();
        GameOver();
    }

    public void GameOver()
    {
        if(PlayerManager.Instance != null)
            PlayerManager.Instance.GameOver();
        Cursor.lockState = CursorLockMode.None;
        PauseGame();
    }
}
