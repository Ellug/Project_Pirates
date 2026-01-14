using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviourPunCallbacks
{
    [Header("Input Length Limit")]
    [SerializeField] private int _minNameLength = 2;
    [SerializeField] private int _maxNameLength = 8;
    [SerializeField] private int _minPwLength = 6;
    [SerializeField] private int _maxPwLength = 18;

    [Header("Reference")]
    [SerializeField] private TitleUI _titleUI;
    [SerializeField] private AuthService _authService;
    [SerializeField] private UserDataStore _userDataStore;

    private bool _isHandling;


    private void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Title);

        InputSystem.actions["Submit"].started += OnClickEnter;

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"Firebase Dependency Error : {task.Result}");
                    return;
                }

                _authService.Initialize();
                _userDataStore.Initialize();
            });
    }

    private void OnDestroy()
    {
        InputSystem.actions["Submit"].started -= OnClickEnter;
    }

    #region 로그인

    public void OnClickConnect()
    {
        HandleLogin();
    }

    private void OnClickEnter(InputAction.CallbackContext ctx)
    {
        HandleLogin();
    }

    private void HandleLogin()
    {
        if (_isHandling) return;

        string email = _titleUI.IdInputField.text;
        string pw = _titleUI.PwInputField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            _titleUI.ShowResult("아이디 또는 비밀번호를 입력해주세요.");
            return;
        }

        _isHandling = true;

        StartCoroutine(_authService.Login(email,
            pw,
            user => { _isHandling = false; OnLoginSuccess(user); },
            error => { _isHandling = false; _titleUI.ShowResult(error); }
            ));
    }

    private void OnLoginSuccess(FirebaseUser user)
    {
        _titleUI.LoginEmail = user.Email;
        _titleUI.IsUserDataMissingAfterLogin = false;

        StartCoroutine(_userDataStore.GetUserData(
            user.UserId,
            userData =>
            {
                string nickname = userData.nickName;

                _titleUI.WelcomeText.text = $"{nickname}님, 환영합니다.";
                _titleUI.WelcomeText.gameObject.SetActive(true);

                ConnectToServer(nickname);
            },
            error =>
            {
                _titleUI.IsUserDataMissingAfterLogin = true;
                _titleUI.ShowResult(error);
            }
        ));
    }

    #endregion

    #region 회원가입

    public void OnClickCheckSignUp()
    {
        bool isOnlyNickname = _titleUI.IsUserDataMissingAfterLogin;

        var input = new SignUpInput
        {
            Email = _titleUI.EmailField.text,
            Password = _titleUI.PasswordField.text,
            PasswordConfirm = _titleUI.PasswordCheckField.text,
            Nickname = _titleUI.NickNameField.text,
            IsNicknameChecked = _titleUI.IsNickNameChecked,

            MinPw = _minPwLength,
            MaxPw = _maxPwLength,
            MinNick = _minNameLength,
            MaxNick = _maxNameLength
        };
        if (!isOnlyNickname)
        {
            var result = new ExceptionChecker<SignUpInput>()
                .AddRule(new SignUpInputChecker())
                .Validate(input);

            if (!result.IsValid)
            {
                _titleUI.ShowResult(result.Message);
                return;
            }
        }
        else //닉네임만 추가
        {
            if (string.IsNullOrEmpty(input.Nickname) || input.Nickname.Length < _minNameLength || input.Nickname.Length > _maxNameLength)
            {
                _titleUI.ShowResult($"닉네임은 {_minNameLength}~{_maxNameLength} 글자여야 합니다.");
                return;
            }
        }

        StartSignUp(
            _titleUI.EmailField.text,
            _titleUI.PasswordField.text,
            _titleUI.NickNameField.text,
            isOnlyNickname
        );
    }

    private void StartSignUp(string email, string pw, string nick, bool isOnlyNickname = false)
    {
        if (_isHandling) return;
        _isHandling = true;

        if (isOnlyNickname)
        {
            StartCoroutine(_userDataStore.CreateUserData(
                _authService.Auth.CurrentUser.UserId,
                nick,
                () =>
                {
                    _isHandling = false;
                    _titleUI.IsSignUpSuccess = true;
                    _titleUI.ShowResult($"닉네임 생성이 완료되었습니다, {nick}님");
                },
                error =>
                {
                    _isHandling = false;
                    _titleUI.ShowResult(error);
                }
            ));
            return;
        }
        else
        {
            StartCoroutine(_authService.SignUp(
                email,
                pw,
                user =>
                {
                    StartCoroutine(CompleteSignUp(user, nick));
                },
                error =>
                {
                    _isHandling = false;

                    var result = new ExceptionChecker<FirebaseException>()
                    .AddRule(new SighUpChecker())
                    .Validate(error);

                    _titleUI.ShowResult(result.Message);
                }
            ));
        }
    }

    private IEnumerator CompleteSignUp(FirebaseUser user, string nick)
    {
        // Auth 닉네임 설정
        var profileTask = user.UpdateUserProfileAsync(
            new UserProfile { DisplayName = nick });

        yield return new WaitUntil(() => profileTask.IsCompleted);

        if (profileTask.Exception != null)
        {
            _isHandling = false;
            _titleUI.ShowResult("닉네임 설정에 실패했습니다.");
            yield break;
        }

        // Firestore 유저 데이터 생성
        yield return _userDataStore.CreateUserData(
            user.UserId,
            nick,
            () =>
            {
                _isHandling = false;
                _titleUI.IsSignUpSuccess = true;
                _titleUI.ShowResult($"생성이 완료되었습니다, {nick}님");
            },
            error =>
            {
                _isHandling = false;
                _titleUI.ShowResult(error);
            });
    }

    #endregion

    #region 닉네임 중복 체크

    public void OnCheckDuplicateName()
    {
        string nickname = _titleUI.NickNameField.text;

        var check = new ExceptionChecker<string>()
            .AddRule(new NicknameChecker(_minNameLength, _maxNameLength))
            .Validate(nickname);

        if (!check.IsValid)
        {
            _titleUI.ShowResult(check.Message);
            return;
        }

        StartCoroutine(_userDataStore.CheckNicknameDuplicate(
            nickname,
            isDuplicated =>
            {
                if (isDuplicated)
                {
                    _titleUI.IsNickNameChecked = false;
                    _titleUI.ShowResult("이미 사용 중인 닉네임입니다.");
                }
                else
                {
                    _titleUI.IsNickNameChecked = true;
                    _titleUI.ShowResult("사용 가능한 닉네임입니다.");
                }
            },
            error =>
            {
                _titleUI.ShowResult(error);
            }));
    }

    #endregion

    #region 네트워크

    private void ConnectToServer(string nickname)
    {
        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        SceneManager.LoadScene("Lobby");
    }

    #endregion
}