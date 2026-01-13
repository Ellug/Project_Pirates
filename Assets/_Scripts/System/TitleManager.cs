using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


// 타이틀 씬 전반적인 관리
// 로그인 기능 같은거 추가 시 네트워크 관련 기능 분리 염두
public class TitleManager : MonoBehaviourPunCallbacks
{
    //[Header("UI References")]
    //[SerializeField] private TMP_InputField _nicknameInput;
    //[SerializeField] private TextMeshProUGUI _errorText;
    //[SerializeField] private TextMeshProUGUI _welcomeText;

    [Header("input Length Limit")]
    [SerializeField] private int _minNameLength = 2;
    [SerializeField] private int _maxNameLength = 8;
    [SerializeField] private int _minPwLength = 6;
    [SerializeField] private int _maxPwLength = 18;

    [SerializeField] private TitleUI _titleUI;

    //서버용 그릇
    public FirebaseAuth _auth;
    public static FirebaseUser user;
    public static FirebaseFirestore _firestore;

    private Coroutine _errorCoroutine;
    private bool _isHandling;
    private string _confirmedNickname = "";

    private float errorMessageLifeTime = 1.8f;

    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Title);
        InputSystem.actions["Submit"].started += OnClickEnter;

        //Firebase 연결
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance; //
                _firestore = FirebaseFirestore.DefaultInstance; //
            }
            else
            {
                Debug.LogError(String.Format("Dependencies 오류." + task.Result));
            }
        });
    }

    private void OnDestroy()
    {
        InputSystem.actions["Submit"].started -= OnClickEnter;
    }

    // 연결 온클릭 이벤트 연결 to 버튼 < 해당 부분 TitleUI로 넘겨야함.
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
        if (_titleUI.IdInputField == null) return;

        _isHandling = true;
        StartCoroutine(CorHandleSubmit());
    }

    private IEnumerator CorHandleSubmit()
    {
        yield return null; // 한 프레임 스킵

        string id = _titleUI.IdInputField.text;
        string pw = _titleUI.PwInputField.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            StartCoroutine(ShowError("아이디 또는 비밀번호를 입력해주세요.", errorMessageLifeTime));
            _isHandling = false;
            yield break;
        }
        StartCoroutine(LoginCor(id, pw));
    }

    IEnumerator LoginCor(string email, string pw)
    {
        if (_auth == null)
        {
            StartCoroutine(ShowError("잠시 후 다시 시도해주세요.", errorMessageLifeTime));
            _isHandling = false;
            yield break;
        }

        Task<AuthResult> loginTask = _auth.SignInWithEmailAndPasswordAsync(email, pw);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            string result = "아이디 또는 비밀번호가 틀렸습니다.";

            StartCoroutine(ShowError(result, errorMessageLifeTime));
            _isHandling = false;
            yield break;
        }

        user = loginTask.Result.User;

        if (user == null)
        {
            StartCoroutine(ShowError("로그인에 실패했습니다.", errorMessageLifeTime));
            _isHandling = false;
            yield break;
        }

        _confirmedNickname = string.IsNullOrEmpty(user.DisplayName) ? "Guest" : user.DisplayName;

        ShowWelcome(_confirmedNickname);

        ConnectToServer(_confirmedNickname);

        _isHandling = false;
    }

    // 닉네임 검증 통과 시 로직 (단순 출력으로 판단하여 완료.)
    private void ShowWelcome(string nickname)
    {
        if (_titleUI.WelcomeText == null)
            return;

        _titleUI.WelcomeText.text = $"{nickname}님, 환영합니다.";
        _titleUI.WelcomeText.gameObject.SetActive(true);
    }

    // 아이디 검증 실패 시 로직
    private IEnumerator ShowError(string txt, float seconds)
    {
        if (_titleUI.ErrorText == null)
            yield break;

        _titleUI.ErrorText.text = txt;
        _titleUI.ErrorText.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);
        _titleUI.ErrorText.gameObject.SetActive(false);
        _errorCoroutine = null;
    }

    #region 회원가입

    #region 회원가입 승인 검증
    public void OnClickCheckSignUp()
    {
        // 이메일 인증
        var emailCheck = new ExceptionChecker<string>()
        .AddRule(new IdChecker())
        .Validate(_titleUI.EmailField.text);

        if (!emailCheck.IsValid)
        {
            ShowResult(emailCheck.Message);
            return;
        }

        // 비밀번호 검증
        var pwCheck = new ExceptionChecker<string>()
            .AddRule(new PasswordChecker(_minPwLength, _maxPwLength))
            .Validate(_titleUI.PasswordField.text);

        if (!pwCheck.IsValid)
        {
            ShowResult(pwCheck.Message);
            return;
        }

        // 비밀번호 확인
        var pwConfirmCheck = new ExceptionChecker<(string, string)>()
            .AddRule(new PasswordConfirmChecker())
            .Validate((_titleUI.PasswordField.text, _titleUI.PasswordCheckField.text));

        if (!pwConfirmCheck.IsValid)
        {
            ShowResult(pwConfirmCheck.Message);
            return;
        }

        // 닉네임 검증
        var nicknameCheck = new ExceptionChecker<string>()
            .AddRule(new NicknameChecker(_minNameLength, _maxNameLength))
            .Validate(_titleUI.NickNameField.text);

        if (!nicknameCheck.IsValid)
        {
            ShowResult(nicknameCheck.Message);
            return;
        }

        // 5. 닉네임 중복 체크 여부
        if (!_titleUI.IsNickNameChecked)
        {
            ShowResult("닉네임 중복 확인을 먼저 해주세요.");
            return;
        }
        StartCoroutine(SignUpCor(_titleUI.EmailField.text, _titleUI.PasswordField.text, _titleUI.NickNameField.text));
    }

    //승인 시 계정 검증
    IEnumerator SignUpCor(string email, string pw, string nick)
    {
        _titleUI.ResultText.text = string.Empty;

        Task<AuthResult> SignUpTask = _auth.CreateUserWithEmailAndPasswordAsync(email, pw);
        yield return new WaitUntil(predicate: () => SignUpTask.IsCompleted);

        if (SignUpTask.Exception != null)
        {
            _titleUI.IsSignUpSuccess = false;

            FirebaseException firebaseEx = SignUpTask.Exception.GetBaseException() as FirebaseException;
            ValidationResult result = new SighUpChecker().Validate(firebaseEx);

            ShowResult(result.Message);
        }
        else
        {
            user = SignUpTask.Result.User;
            if (user == null)
            {
                _titleUI.IsSignUpSuccess = false;
                ShowResult("유저 생성에 실패했습니다.");
                yield break;
            }

            // 닉네임 설정
            Task profileTask = user.UpdateUserProfileAsync(new UserProfile { DisplayName = nick });

            yield return new WaitUntil(() => profileTask.IsCompleted);

            if (profileTask.Exception != null)
            {
                _titleUI.IsSignUpSuccess = false;
                ShowResult("닉네임 설정에 실패하였습니다.");
                yield break;
            }
            string uuid = user.UserId;

            var userData = new Dictionary<string, object>
            {
                { "uuid", uuid },
                { "nickName", nick },
                { "win", 0 },
                { "lose", 0 }
            };

            Task firestoreTask = _firestore
                .Collection("users")
                .Document(uuid)
                .SetAsync(userData);
            yield return new WaitUntil(() => firestoreTask.IsCompleted);

            if (firestoreTask.Exception != null)
            {
                Debug.LogError(firestoreTask.Exception);
                _titleUI.IsSignUpSuccess = false;
                ShowResult("유저 데이터 저장에 실패했습니다.");
                yield break;
            }
            // 성공
            _titleUI.IsSignUpSuccess = true;
            ShowResult($"생성이 완료되었습니다, 반갑습니다 {user.DisplayName}님");
        }
    }

    #endregion

    #region 중복 닉네임 검증
    public void OnCheckDuplicateName()
    {
        string nickname = _titleUI.NickNameField.text;

        var checkResult = new ExceptionChecker<string>()
        .AddRule(new NicknameChecker(_minNameLength, _maxNameLength))
        .Validate(nickname);

        if (!checkResult.IsValid)
        {
            ShowResult(checkResult.Message);
            return;
        }

        StartCoroutine(CheckDuplicateNicknameCor(nickname));
    }

    IEnumerator CheckDuplicateNicknameCor(string nickname)
    {
        _titleUI.ResultText.text = string.Empty;

        var query = _firestore
            .Collection("users")
            .WhereEqualTo("nickName", nickname)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null)
        {
            Debug.LogError(query.Exception);
            ShowResult("닉네임 중복 확인 중 오류가 발생했습니다.");
            yield break;
        }

        if (query.Result.Count > 0)
        {
            _titleUI.IsNickNameChecked = false;
            ShowResult("이미 사용 중인 닉네임입니다.");
        }
        else
        {
            _titleUI.IsNickNameChecked = true;
            ShowResult("사용 가능한 닉네임입니다.");
        }
    }

    public void OnNameValueChanged()
    {
        _titleUI.IsNickNameChecked = false;
    }

    private void ShowResult(string msg)
    {
        _titleUI.ResultText.text = msg;
        _titleUI.ResultPanel.SetActive(true);
    }
    #endregion
    #endregion

    #region 서버 연결
    // 서버 연결
    public void ConnectToServer(string nickname)
    {
        Debug.Log($"Connect To Server : {nickname}");
        PhotonNetwork.NickName = nickname; //네트워크에 사용될 닉네임? 여기에는 UserManger(SingleTon)으로 값 넘겨주면 될듯
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 연결 콜백
    public override void OnConnectedToMaster()
    {
        Debug.Log("CB : Complete Connect to Master");
        SceneManager.LoadScene("Lobby");
    }
    #endregion
}
