using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun
{
    public static GameObject LocalInstancePlayer;

    [HideInInspector] public bool isMafia;

    private Vector2 _mouseDelta;
    private float _xRotation;
    private Camera _camera;
    private PhotonView _view;
    private PlayerInteraction _playerInteraction;
    private PlayerModel _model;

    private ExitGames.Client.Photon.Hashtable _table;
    
    // Player State
    public PlayerStateMachine StateMachine { get; private set; }
    public IdleState StateIdle { get; private set; }
    public MoveState StateMove { get; private set; }
    public JumpState StateJump { get; private set; }
    public CrouchState StateCrouch { get; private set; }
    public AttackState StateAttack { get; private set; }
    public DeathState StateDeath { get; private set; }

    public Vector2 InputMove { get; private set; }
    public bool InputJump { get; private set; }
    public bool InputKnockBack { get; private set; }
    public bool InputAttack { get; private set; }
    public JobId PlayerJob { get; private set; }

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

        _model = GetComponent<PlayerModel>();

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

        InputJump = false;
        InputMove = Vector2.zero;

        // 생성된 사람 출석 체크
        _table = new ExitGames.Client.Photon.Hashtable {
                { "IsMafia", true }
            };
        PhotonNetwork.LocalPlayer.SetCustomProperties(_table);
        PlayerManager.Instance.photonView.RPC("PlayerEnterCheck", RpcTarget.MasterClient);
    }

    void Start()
    {
        PlayerManager.Instance.RegistLocalPlayer(this);
        _playerInteraction = GetComponent<PlayerInteraction>();
        FindFirstObjectByType<InGameManager>().RegistPlayer(this);

        StateIdle = new IdleState(this);
        StateMove = new MoveState(this);
        StateJump = new JumpState(this);
        StateCrouch = new CrouchState(this);
        StateAttack = new AttackState(this);
        StateDeath = new DeathState(this);

        StateMachine = new PlayerStateMachine(StateIdle);

        InputSystem.actions["Move"].performed += OnMove;
        InputSystem.actions["Move"].canceled += OnMove;
        InputSystem.actions["Sprint"].performed += OnSprint;
        InputSystem.actions["Sprint"].canceled += OnSprint;
        InputSystem.actions["Crouch"].performed += OnCrouch;
        InputSystem.actions["Crouch"].canceled += OnCrouch;
        InputSystem.actions["Attack"].started += OnAttack;
        InputSystem.actions["KnockBack"].started += OnKnockBack;

        InputSystem.actions["Look"].performed += OnLook;
        InputSystem.actions["Look"].canceled += OnLook;
        InputSystem.actions["Interact"].started += OnInteraction;
        //InputSystem.actions["Jump"].started += OnJump;
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
        InputSystem.actions["KnockBack"].started -= OnKnockBack;

        InputSystem.actions["Look"].performed -= OnLook;
        InputSystem.actions["Look"].canceled -= OnLook;
        InputSystem.actions["Interact"].started -= OnInteraction;
        //InputSystem.actions["Jump"].started -= OnJump;
    }

    private void Update()
    {
        StateMachine.CurrentState.FrameUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    private void LateUpdate()
    {
        PlayerLook();
    }

    private void PlayerLook()
    {
        float mouseX = _mouseDelta.x * _model.mouseSensitivity * Time.deltaTime;
        float mouseY = _mouseDelta.y * _model.mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f); // 위아래 90도 제한

        // 카메라와 플레이어 몸체에 회전 적용
        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f); // 카메라만 위아래로
        transform.Rotate(Vector3.up * mouseX); // 플레이어 몸체 전체가 좌우로 회전
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        InputMove = ctx.ReadValue<Vector2>();
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
            _model.IsRunning = true;
        else
            _model.IsRunning = false;
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            _model.IsCrouching = true;
        else
            _model.IsCrouching = false;
    }
    private void OnJump(InputAction.CallbackContext ctx)
    {
        InputJump = true;
    }

    // 공격, 밀기, 직업 스킬 키가 무시되는 조건
    // 1. 공중에 떠 있을 때
    // 2. 앉아 있을 때
    private void OnAttack(InputAction.CallbackContext ctx)
    {
        InputAttack = true;
    }

    private void OnKnockBack(InputAction.CallbackContext ctx)
    {
        InputKnockBack = true;
    }

    public void SetInitInput()
    {
        InputAttack = false;
        InputKnockBack = false;
    }

    public BaseJob GetPlayerJob()
    {
        return _model.MyJob;
    }

    [PunRPC]
    public void IsMafia()
    {
        Debug.Log("당신은 마피아입니다.");
        isMafia = true;
    }

    [PunRPC]
    public void AssignJob(int jobId)
    {
        JobId myJob = (JobId)jobId;
        _model.AssignJob(myJob);
        PlayerJob = myJob;
    }
}
