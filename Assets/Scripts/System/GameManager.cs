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
}
