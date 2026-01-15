using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerContoller : MonoBehaviourPunCallbacks
{
    public static GameObject LocalInstancePlayer;

    [SerializeField] private float _mouseSensitivity;

    private Vector2 _mouseDelta;
    private float _xRotation;
    private Camera _camera;
    private PhotonView _view;
    private PlayerInteraction _playerInteraction;
    private PlayerStateMachine _stateMachine;

    // Player State
    private IdleState _idle;
    private MoveState _move;
    private JumpState _jump;
    private CrouchState _crouch;

    public float walkSpeed;
    public float runSpeed;
    public float crouchSpeed;
    public float jumpPower;
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

        if (!_view.IsMine)
            return;

        Cursor.lockState = CursorLockMode.Locked;

        _camera = Camera.main;
        _camera.transform.SetParent(transform, false);
        _camera.transform.localPosition = new Vector3(0f, 1.77f, 0f);

        InputMove = Vector2.zero;
        IsGrounded = true;
    }

    void Start()
    {
        if (!_view.IsMine) 
            return;

        Animator = GetComponent<Animator>();
        _playerInteraction = GetComponent<PlayerInteraction>();

        _idle = new IdleState(this);
        _move = new MoveState(this);
        _jump = new JumpState(this);
        _crouch = new CrouchState(this);

        _stateMachine = new PlayerStateMachine(_idle);

        InputSystem.actions["Move"].performed += OnMove;
        InputSystem.actions["Move"].canceled += OnMove;
        InputSystem.actions["Sprint"].performed += OnSprint;
        InputSystem.actions["Sprint"].canceled += OnSprint;
        InputSystem.actions["Crouch"].performed += OnCrouch;
        InputSystem.actions["Crouch"].canceled += OnCrouch;

        InputSystem.actions["Look"].performed += OnLook;
        InputSystem.actions["Look"].canceled += OnLook;
        InputSystem.actions["Interact"].started += OnInteraction;
        InputSystem.actions["Jump"].started += OnJump;
    }

    void OnDestroy()
    {
        if (!_view.IsMine)
            return;

        Cursor.lockState = CursorLockMode.None;
        InputSystem.actions["Move"].performed -= OnMove;
        InputSystem.actions["Move"].canceled -= OnMove;
        InputSystem.actions["Sprint"].performed -= OnSprint;
        InputSystem.actions["Sprint"].canceled -= OnSprint;
        InputSystem.actions["Crouch"].performed -= OnCrouch;
        InputSystem.actions["Crouch"].canceled -= OnCrouch;

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

    public void OnMove(InputAction.CallbackContext ctx)
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
}
