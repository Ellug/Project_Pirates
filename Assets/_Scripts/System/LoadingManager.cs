using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    private const string LOADED_KEY = "OnLoaded";

    [Header("Loading")]
    [SerializeField] private TextMeshProUGUI _loadingStatusText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private Slider _loadingbar;
    [SerializeField] private TextMeshProUGUI _countdownText;

    [SerializeField] private float _currentDisplayProgress;
    [SerializeField] private float _fillSpeed = 0.5f;

    [Header("Tips")]
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private float _tipChangeInterval = 5f;
    [SerializeField] private string[] _tips = { };

    private readonly ExitGames.Client.Photon.Hashtable _loadedTrue =
        new ExitGames.Client.Photon.Hashtable { { LOADED_KEY, true } };
    private readonly ExitGames.Client.Photon.Hashtable _loadedFalse =
        new ExitGames.Client.Photon.Hashtable { { LOADED_KEY, false } };

    private bool _isAllReady = false;

    void Start()
    {
        ResetLoadedFlag();

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete += TriggerIsAllReady;
        StartCoroutine(LoadingScene());
        StartCoroutine(StartTips());
    }

    void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.allReadyComplete -= TriggerIsAllReady;
    }

    IEnumerator StartTips()
    {
        if (_tips.Length == 0) yield break;

        int currentIndex = Random.Range(0, _tips.Length);

        while (true)
        {
            _tipText.text = _tips[currentIndex];

            yield return new WaitForSeconds(_tipChangeInterval);

            currentIndex = (currentIndex + 1) % _tips.Length;
        }
    }

    IEnumerator LoadingScene()
    {
        // 시작하자마자 게임 씬을 비동기로 로드를 시작하고 씬 전환을 대기한다.
        AsyncOperation operation = SceneManager.LoadSceneAsync("InGame");
        operation.allowSceneActivation = false;
        _loadingbar.value = 1;

        yield return new WaitForSeconds(1f);

        // 진행도를 보여준다.
        while (_currentDisplayProgress < 1f)
        {
            // 실제 로딩 수치를 목표값으로 설정 (0.9가 끝이므로 0.9로 나눠서 1.0 기준으로 보정)
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            if (_currentDisplayProgress < targetProgress)
            {
                _currentDisplayProgress += Time.deltaTime * _fillSpeed;
            }

            if (operation.progress >= 0.9f)
            {
                _currentDisplayProgress += Time.deltaTime * _fillSpeed;
            }

            _currentDisplayProgress = Mathf.Clamp01(_currentDisplayProgress);

            _progressText.text = $"{(int)(_currentDisplayProgress * 100)} %";
            _loadingbar.value = 1f - _currentDisplayProgress;

            yield return null;
        }

        _loadingbar.value = 0f;
        _loadingStatusText.text = "Waiting...";
        _progressText.text = "100 %";
        // 여기서 로컬 로딩이 끝났으니 커스텀 프로퍼티를 바꾼다.
        // 이걸 마스터 클라이언트가 감지할 것이다.
        yield return null;

        PhotonNetwork.LocalPlayer.SetCustomProperties(_loadedTrue);

        // 그리고 누군가 게임씬을 전환하라고 신호를 줄 때까지 기다린다.
        yield return new WaitUntil(() => _isAllReady);

        int countDown = 3;
        _loadingStatusText.text = "Starting...";
        while (countDown > 0) 
        {
            _countdownText.text = $"{countDown--}";
            yield return new WaitForSeconds(1f);
        }

        operation.allowSceneActivation = true;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (changedProps.TryGetValue(LOADED_KEY, out object value))
        {
            if ((bool)value == true) 
                PlayerManager.Instance.onLoadedPlayer++;
        }
    }

    public void TriggerIsAllReady()
    {
        _isAllReady = true;
    }

    private void ResetLoadedFlag()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_loadedFalse);
    }
}
