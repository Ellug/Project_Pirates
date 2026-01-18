using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TitleUI : MonoBehaviour
{
    [Header("Login Window")]
    [SerializeField] private TMP_InputField _idInputField;
    [SerializeField] private TMP_InputField _pwInputField;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private TextMeshProUGUI _welcomeText;

    [Header("Panel")]
    [SerializeField] private GameObject _authPanel;
    [SerializeField] private GameObject _signUpPanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("InputField")]
    [SerializeField] private TMP_InputField _emailField;
    [SerializeField] private TMP_InputField _passwordField;
    [SerializeField] private TMP_InputField _passwordCheckField;
    [SerializeField] private TMP_InputField _nickNameField;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _resultText;

    private bool _isSignUpSuccess = false;
    private bool _isNickNameChecked = false;
    private bool _isUserDataMissingAfterLogin = false;
    private string _loginEmail;

#region 프로퍼티
    // Login Window
    public TMP_InputField IdInputField => _idInputField;
    public TMP_InputField PwInputField => _pwInputField;
    public TextMeshProUGUI ErrorText => _errorText;
    public TextMeshProUGUI WelcomeText => _welcomeText;

    // Panel
    public GameObject AuthPanel => _authPanel;
    public GameObject SignUpPanel => _signUpPanel;
    public GameObject ResultPanel => _resultPanel;

    // InputField
    public TMP_InputField EmailField => _emailField;
    public TMP_InputField PasswordField => _passwordField;
    public TMP_InputField PasswordCheckField => _passwordCheckField;
    public TMP_InputField NickNameField => _nickNameField;

    // Text
    public TextMeshProUGUI ResultText => _resultText;

    // Status
    public bool IsSignUpSuccess
    {
        get => _isSignUpSuccess;
        set => _isSignUpSuccess = value;
    }
    public bool IsNickNameChecked
    {
        get => _isNickNameChecked;
        set => _isNickNameChecked = value;
    } 
    public bool IsUserDataMissingAfterLogin
    {
        get => _isUserDataMissingAfterLogin;
        set => _isUserDataMissingAfterLogin = value;
    }
    public string LoginEmail
    {
        get => _loginEmail;
        set => _loginEmail = value;
    }
#endregion

    private void Start()
    {
        if (_welcomeText != null)
            _welcomeText.gameObject.SetActive(false);

        if (_errorText != null)
            _errorText.gameObject.SetActive(false);

        if (_signUpPanel != null)
            _signUpPanel.SetActive(false);
        
        if (_resultPanel != null)
            _resultPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.tabKey.wasPressedThisFrame) return;

        MoveByNextNavi();
    }

    private void MoveByNextNavi()
    {
        if (EventSystem.current == null) return;

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return;

        // 현재 선택된 UI 가 셀렉터블인지?
        var cur = go.GetComponent<Selectable>();
        if (cur == null) return;

        Selectable next = cur.FindSelectableOnDown();
        if (next == null) return;

        next.Select();

        // 다음이 인풋필드이면 입력 활성화
        var nextTmp = next.GetComponent<TMP_InputField>();
        if (nextTmp != null)
            nextTmp.ActivateInputField();
    }

    public void OnClickSignUp()
    {
        _emailField.text = string.Empty;
        _passwordField.text = string.Empty;
        _passwordCheckField.text = string.Empty;
        _nickNameField.text = string.Empty;
        _authPanel.SetActive(false);
        _signUpPanel.SetActive(true);
    }

    public void OnExitSignUp()
    {
        _signUpPanel.SetActive(false);
        if (_resultPanel.activeSelf == true)
        {
            _resultPanel.SetActive(false);
        }
        _authPanel.SetActive(true);
    }

    public void OnResultCheck()
    {
        _resultPanel.SetActive(false);
        if (_isUserDataMissingAfterLogin)
        {
            _authPanel.SetActive(false);
            _signUpPanel.SetActive(true);

            _emailField.text = _loginEmail;

            _emailField.interactable = false;
            _passwordField.interactable = false;
            _passwordCheckField.interactable = false;
        }
        if (_isSignUpSuccess == true)
        {
            _signUpPanel.SetActive(false);
            _authPanel.SetActive(true);
        }
    }

    public void OnNameValueChanged()
    {
        _isNickNameChecked = false;
    }

    public void ShowResult(string msg)
    {
        _resultText.text = msg;
        _resultPanel.SetActive(true);
    }
}
