using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI _loadingStatusText;
    [SerializeField] private TextMeshProUGUI _progressText;

    private ExitGames.Client.Photon.Hashtable _table = 
        new ExitGames.Client.Photon.Hashtable 
    {
        { "OnLoaded", true }
    };

    WaitForSeconds _delay = new WaitForSeconds(1f);

    private bool _isAllReady = false;

    void Start()
    {
        PlayerManager.Instance.allReadyComplete += TriggerIsAllReady;
        StartCoroutine(LoadingScene());
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.allReadyComplete -= TriggerIsAllReady;
    }

    IEnumerator LoadingScene()
    {
        // 시작하자마자 게임 씬을 비동기로 로드를 시작하고 씬 전환을 대기한다.
        AsyncOperation operation = SceneManager.LoadSceneAsync("InGame");
        operation.allowSceneActivation = false;

        // 진행도를 보여준다.
        while (operation.progress < 0.9f)
        {
            _progressText.text = $"{(int)(operation.progress * 100)} %";
            yield return _delay;
        }

        _progressText.text = "100 %";
        _loadingStatusText.text = "Waiting...";
        // 여기서 로컬 로딩이 끝났으니 커스텀 프로퍼티를 바꾼다.
        // 이걸 마스터 클라이언트가 감지할 것이다.
        yield return null;

        PhotonNetwork.LocalPlayer.SetCustomProperties(_table);

        // 그리고 누군가 게임씬을 전환하라고 신호를 줄 때까지 기다린다.
        yield return new WaitUntil(() => _isAllReady);

        int countDown = 3;
        _loadingStatusText.text = "Starting...";
        while (countDown > 0) 
        {
            _progressText.text = $"{countDown--}";
            yield return _delay;
        }

        operation.allowSceneActivation = true;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (changedProps.TryGetValue("OnLoaded", out object value))
        {
            if ((bool)value == true) 
                PlayerManager.Instance.onLoadedPlayer++;
        }
    }

    public void TriggerIsAllReady()
    {
        _isAllReady = true;
    }
}
