using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

// 타이틀 씬 전반적인 관리
// 로그인 기능 같은거 추가 시 네트워크 관련 기능 분리 염두
public class TitleManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private TextMeshProUGUI _welcomeText;

    [Header("Nickname Length Limit")]
    [SerializeField] private int _minLength = 2;
    [SerializeField] private int _maxLength = 8;

    private Coroutine _errorCoroutine;
    private bool _isHandling;
    private string _confirmedNickname = "";
    private NicknameChecker _nickChecker;
    
    private float errorMessageLifeTime = 1.8f;

    void Start()
    {
        _nickChecker = new NicknameChecker(_minLength, _maxLength);
        GameManager.Instance.SetSceneState(SceneState.Title);
        InputSystem.actions["Submit"].started += OnClickEnter;

        if (_welcomeText != null)
            _welcomeText.gameObject.SetActive(false);

        if (_errorText != null)
            _errorText.gameObject.SetActive(false);

        if (_nicknameInput != null)
            _nicknameInput.characterLimit = _maxLength;
    }

    private void OnDestroy()
    {
        InputSystem.actions["Submit"].started -= OnClickEnter;
    }

    // 연결 온클릭 이벤트 연결 to 버튼
    public void OnClickConnect()
    {
        HandleSubmit();
    }
    private void OnClickEnter(InputAction.CallbackContext ctx)
    {
        HandleSubmit();
    }

    // 제출 메서드
    private void HandleSubmit()
    {
        if (_isHandling) return; // 이미 처리 중이면 무시
        if (_nicknameInput == null) return;

        _isHandling = true;
        StartCoroutine(CorHandleSubmit());
    }

    private IEnumerator CorHandleSubmit()
    {
        yield return null; // 한 프레임 스킵

        // 확정 닉네임 없으면 Enter/Btn 모두 확정 시도
        if (string.IsNullOrEmpty(_confirmedNickname))
        {
            // IME 조합 완료 대기
            yield return StartCoroutine(CorConfirmIme());

            // 현재 입력값으로 검증 시도.
            // 참이면 통과, 거짓이면 에러 메시지 잠시 띄우고 사라짐
            if ( _nickChecker.TryConfirmCurrentInput(_nicknameInput.text, out string resultMsg) )
            {
                Debug.Log(resultMsg);
                _confirmedNickname = _nicknameInput.text;
                ShowWelcome(_nicknameInput.text);
            }
            else
            {
                if (_errorCoroutine != null)
                    StopCoroutine(_errorCoroutine);

                _errorCoroutine = StartCoroutine(ShowError(resultMsg, errorMessageLifeTime));
                _isHandling = false;
                yield break;
            }
        }

        // 확정 닉네임이 있으면 연결 시도
        ConnectToServer(_confirmedNickname);
        _isHandling = false;
    }

    // 한 프레임 쉬고 포커스 해제서 IME 조합 완료 유도
    public IEnumerator CorConfirmIme()
    {
        if (_nicknameInput == null)
            yield break;

        // 한 프레임 넘겨
        yield return null;

        // 포커스 해제
        if (_nicknameInput.isFocused)
            _nicknameInput.DeactivateInputField(false);

        _nicknameInput.ForceLabelUpdate();
    }

    // 닉네임 검증 통과 시 로직
    private void ShowWelcome(string nickname)
    {
        if (_welcomeText == null)
            return;

        _welcomeText.text = $"{nickname}님, 환영합니다.";
        _welcomeText.gameObject.SetActive(true);
    }

    // 닉네임 검증 실패 시 로직
    private IEnumerator ShowError(string txt, float seconds)
    {
        if (_errorText == null)
            yield break;

        _errorText.text = txt;
        _errorText.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);
        _errorText.gameObject.SetActive(false);
        _errorCoroutine = null;
    }

    // 서버 연결
    public void ConnectToServer(string nickname)
    {
        Debug.Log($"Connect To Server : {nickname}");
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 연결 콜백
    public override void OnConnectedToMaster()
    {
        Debug.Log("CB : Complete Connect to Master");
        SceneManager.LoadScene("Lobby");
    }
}
