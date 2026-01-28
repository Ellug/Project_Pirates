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

    //private GameResultController _controller;

    private FadeController _fadeController;

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
    public void SetFadingController(FadeController fadeController)
    {
        _fadeController = fadeController;
    }

    public void EndGame(bool isWin)
    {
        _fadeController.EndInGameFade(isWin);
        GameOver();
    }

    public void GameOver()
    {
        if(PlayerManager.Instance != null)
            PlayerManager.Instance.GameOver();
    }
}
