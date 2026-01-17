using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerContoller : MonoBehaviourPunCallbacks
{
    public static GameObject LocalInstancePlayer;

    [SerializeField] private float _mouseSensitivity;
    public float knockBackForce = 5f;

    private Vector2 _mouseDelta;
    private float _xRotation;
    private Camera _camera;
    private PhotonView _view;
    private PlayerInteraction _playerInteraction;
    private PlayerStateMachine _stateMachine;
    private PlayerModel _model;

    // Player State
    private IdleState _idle;
    private MoveState _move;
    private JumpState _jump;
    private CrouchState _crouch;
    private AttackState _attack;

    private ExitGames.Client.Photon.Hashtable _table;

    public float walkSpeed;
    public float runSpeed;
    public float crouchSpeed;
    public float jumpPower;

    // 마피아 여부 (임시)
    public bool isMafia;

    public Animator Animator { get; private set; }
    public Vector2 InputMove { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsGrounded { get; set; }

    public readonly string animNameOfMove = "MoveValue";
    public readonly string animNameOfRun = "Running";
    public readonly string animNameOfCrouch = "Crouching";
    public readonly string animNameOfJump = "Jumping";

    private void Awake()
    {
        _view = GetComponent<PhotonView>();

        // 내 것이 아니면 컴포넌트를 아예 비활성화
        // 다른 사람의 Update, FixedUpdate 같은 것들이 호출 자체가 안됨
        if (!_view.IsMine)
        {
            _playerInteraction = GetComponent<PlayerInteraction>();
            _playerInteraction.enabled = false;
            this.enabled = false;
            return;
        }

        // 내 아바타는 숨김 처리한다.
        // 다른 사람 아바타는 볼 수 있고 내 아바타도 남한테는 보인다.
        SkinnedMeshRenderer[] myAvatar =
            transform.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var avatar in myAvatar) 
        {
            avatar.enabled = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        isMafia = false;

        _camera = Camera.main;
        _camera.transform.SetParent(transform, false);
        _camera.transform.localPosition = new Vector3(0f, 1.77f, 0f);

        InputMove = Vector2.zero;
        IsGrounded = true;

        // 생성된 사람 출석 체크
        _table = new ExitGames.Client.Photon.Hashtable {
                { "IsMafia", true }
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(_table);
        PlayerManager.Instance.photonView.RPC("PlayerEnterCheck", RpcTarget.MasterClient);
    }

    void Start()
    {
        Animator = GetComponent<Animator>();
        _playerInteraction = GetComponent<PlayerInteraction>();
        FindFirstObjectByType<InGameManager>().RegistPlayer(this);

        _idle = new IdleState(this);
        _move = new MoveState(this);
        _jump = new JumpState(this);
        _crouch = new CrouchState(this);
        _attack = new AttackState(this);

        _stateMachine = new PlayerStateMachine(_idle);

        InputSystem.actions["Move"].performed += OnMove;
        InputSystem.actions["Move"].canceled += OnMove;
        InputSystem.actions["Sprint"].performed += OnSprint;
        InputSystem.actions["Sprint"].canceled += OnSprint;
        InputSystem.actions["Crouch"].performed += OnCrouch;
        InputSystem.actions["Crouch"].canceled += OnCrouch;
        InputSystem.actions["Attack"].started += OnAttack;

        InputSystem.actions["Look"].performed += OnLook;
        InputSystem.actions["Look"].canceled += OnLook;
        InputSystem.actions["Interact"].started += OnInteraction;
        InputSystem.actions["Jump"].started += OnJump;
    }

    void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        InputSystem.actions["Move"].performed -= OnMove;
        InputSystem.actions["Move"].canceled -= OnMove;
        InputSystem.actions["Sprint"].performed -= OnSprint;
        InputSystem.actions["Sprint"].canceled -= OnSprint;
        InputSystem.actions["Crouch"].performed -= OnCrouch;
        InputSystem.actions["Crouch"].canceled -= OnCrouch;
        InputSystem.actions["Attack"].started -= OnAttack;

        InputSystem.actions["Look"].performed -= OnLook;
        InputSystem.actions["Look"].canceled -= OnLook;
        InputSystem.actions["Interact"].started -= OnInteraction;
        InputSystem.actions["Jump"].started -= OnJump;
    }

    private void Update()
    {
        _stateMachine.CurrentState.FrameUpdate();
    }

    void FixedUpdate()
    {
        _stateMachine.CurrentState.PhysicsUpdate();
    }

    private void LateUpdate()
    {
        PlayerLook();
    }

    private void PlayerLook()
    {
        float mouseX = _mouseDelta.x * _mouseSensitivity * Time.deltaTime;
        float mouseY = _mouseDelta.y * _mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f); // 위아래 90도 제한

        // 카메라와 플레이어 몸체에 회전 적용
        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f); // 카메라만 위아래로
        transform.Rotate(Vector3.up * mouseX); // 플레이어 몸체 전체가 좌우로 회전
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        InputMove = ctx.ReadValue<Vector2>();
        if (ctx.performed)
            _stateMachine.ChangeState(_move);
        else
        {
            if (IsCrouching)
                _stateMachine.ChangeState(_crouch);
            else
                _stateMachine.ChangeState(_idle);
        }
    }

    private void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (_playerInteraction.IsInteractable)
            _playerInteraction.InteractObj();
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        _mouseDelta = ctx.ReadValue<Vector2>();
    }
    private void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            IsRunning = true;
        else 
            IsRunning = false;
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        // 공격키가 무시되는 조건
        // 1. 공중에 떠 있을 때
        // 2. 앉아 있을 때
        if (IsCrouching) return;
        else if (!IsGrounded) return;

        _stateMachine.ChangeState(_attack);
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            IsCrouching = true;
            if (_stateMachine.CurrentState == _idle)
                _stateMachine.ChangeState(_crouch);
        }
        else
        {
            IsCrouching = false;
            if (_stateMachine.CurrentState == _crouch)
                _stateMachine.ChangeState(_idle);
        }   
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        _stateMachine.ChangeState(_jump);
    }

    [PunRPC]
    public void IsMafia()
    {
        Debug.Log("당신은 마피아입니다.");
        isMafia = true;
    }
}
