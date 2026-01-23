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

    // 로컬 플레이어의 입력을 차단/복구하기 위해 PlayerInput.actions로 교체
    private PlayerInput _playerInput;
    private InputAction _actMove;
    private InputAction _actSprint;
    private InputAction _actCrouch;
    private InputAction _actAttack;
    private InputAction _actKnockBack;
    private InputAction _actLook;
    private InputAction _actInteract;
    // private InputAction _actJump;
    private InputAction _actJobSkill;
    private InputAction _actUseFirstItem;
    private InputAction _actUseSecondItem;

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
            // 네트워크 섞임 방지 => 다른 플레이어 PI 비활성화
            var remotePI = GetComponent<PlayerInput>();
            if (remotePI != null) remotePI.enabled = false;

            _playerInteraction = GetComponent<PlayerInteraction>();
            _playerInteraction.enabled = false;
            this.enabled = false;
            return;
        }

        _playerInput = GetComponent<PlayerInput>();

        // 로컬 플레이어 PlayerInput을 InputManager에 등록
        // 옵션/콘솔 열리면 InputManager가 ActionMap을 UI로 바꿔서 플레이어 입력 자동 차단
        if (InputManager.Instance != null && _playerInput != null)
            InputManager.Instance.RegisterLocalPlayer(_playerInput);

        _model = GetComponent<PlayerModel>();

        // 내 아바타는 숨김 처리한다.
        // 다른 사람 아바타는 볼 수 있고 내 아바타도 남한테는 보인다.
        SkinnedMeshRenderer[] myAvatar =
            transform.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var avatar in myAvatar)
        {
            avatar.enabled = false;
        }

        // ===== [정리] 커서 Lock은 InputManager가 담당하므로 여기서 설정하지 않음 =====
        // Cursor.lockState = CursorLockMode.Locked;
        isMafia = false;

        _camera = Camera.main;
        _camera.transform.SetParent(transform, false);
        _camera.transform.localPosition = new Vector3(0f, 1.77f, 0f);

        InputJump = false;
        InputMove = Vector2.zero;
    }

    private void Start()
    {
        PlayerManager.Instance.RegistLocalPlayer(this);
        _playerInteraction = GetComponent<PlayerInteraction>();
        FindFirstObjectByType<InGameManager>().RegistPlayer(this);

        // 생성된 사람 출석 체크
        _table = new ExitGames.Client.Photon.Hashtable
        {
            { "IsMafia", true }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(_table);
        PlayerManager.Instance.photonView.RPC("PlayerEnterCheck", RpcTarget.MasterClient);

        // 상태 클래스 할당
        StateIdle = new IdleState(this);
        StateMove = new MoveState(this);
        StateJump = new JumpState(this);
        StateCrouch = new CrouchState(this);
        StateAttack = new AttackState(this);
        StateDeath = new DeathState(this);

        StateMachine = new PlayerStateMachine(StateIdle);

        if (!_view.IsMine) return;

        var hud = FindFirstObjectByType<PlayerHUD>();
        hud.Bind(_model);

        ItemEffects effects = new ItemEffects();
        effects.Initialize();
        _model.RegistItemEffects(effects);

        // PlayerInput.actions 기반 구독
        // InputManager가 SwitchCurrentActionMap("UI")로 바꾸면 Player 맵 입력 차단 (옵션/콘솔에서 플레이어 조작 불가)

        if (_playerInput != null)
        {
            var a = _playerInput.actions;

            _actMove = a["Move"];
            _actSprint = a["Sprint"];
            _actCrouch = a["Crouch"];
            _actAttack = a["Attack"];
            _actKnockBack = a["KnockBack"];
            _actLook = a["Look"];
            _actInteract = a["Interact"];
            // _actJump = a["Jump"];
            _actJobSkill = a["JobSkill"];
            _actUseFirstItem = a["UseFirstItem"];
            _actUseSecondItem = a["UseSecondItem"];

            // Add
            _actMove.performed += OnMove;
            _actMove.canceled += OnMove;

            _actSprint.performed += OnSprint;
            _actSprint.canceled += OnSprint;

            _actCrouch.performed += OnCrouch;
            _actCrouch.canceled += OnCrouch;

            _actAttack.started += OnAttack;
            _actKnockBack.started += OnKnockBack;

            _actLook.performed += OnLook;
            _actLook.canceled += OnLook;

            _actInteract.started += OnInteraction;
            // _actJump.started += OnJump;
            _actJobSkill.started += OnJobSkill;

            _actUseFirstItem.started += OnUseFirstItem;
            _actUseSecondItem.started += OnUseSecondItem;
        }
    }

    void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;

        // 씬 전환 시 InputManager가 파괴된 PlayerInput 참조를 들고 있지 않도록 함
        if (InputManager.Instance != null && _playerInput != null)
            InputManager.Instance.UnregisterLocalPlayer(_playerInput);

        // PlayerInput.actions 해제
        if (_actMove != null)
        {
            _actMove.performed -= OnMove;
            _actMove.canceled -= OnMove;
        }

        if (_actSprint != null)
        {
            _actSprint.performed -= OnSprint;
            _actSprint.canceled -= OnSprint;
        }

        if (_actCrouch != null)
        {
            _actCrouch.performed -= OnCrouch;
            _actCrouch.canceled -= OnCrouch;
        }

        if (_actAttack != null)
            _actAttack.started -= OnAttack;

        if (_actKnockBack != null)
            _actKnockBack.started -= OnKnockBack;

        if (_actLook != null)
        {
            _actLook.performed -= OnLook;
            _actLook.canceled -= OnLook;
        }

        if (_actInteract != null)
            _actInteract.started -= OnInteraction;

        // if (_actJump != null)
        //     _actJump.started -= OnJump;

        if (_actJobSkill != null)
            _actJobSkill.started -= OnJobSkill;
    }

    private void OnJobSkill(InputAction.CallbackContext _)
    {
        _model.MyJob.UniqueSkill();
    }

    private void Update()
    {
        StateMachine.CurrentState.FrameUpdate();
    }

    void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();

        if (_model != null && !_model.IsRunning)
            _model.RecoverStamina(_model.StaminaRecoverPerSec * Time.fixedDeltaTime);
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
    public void UpdateSensitivity(float value)
    {
        _model.mouseSensitivity = value;

        PlayerPrefs.SetFloat("MouseSensitivity", value);
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

    // 아이템 사용 키

    private void OnUseFirstItem(InputAction.CallbackContext ctx)
    {
        _model.TryUseItem(0);
    }

    private void OnUseSecondItem(InputAction.CallbackContext ctx)
    {
        _model.TryUseItem(1);
    }

    // 인풋 초기화 (버그 방지)
    public void SetInitInput()
    {
        InputAttack = false;
        InputKnockBack = false;
    }

    public BaseJob GetPlayerJob()
    {
        return _model.MyJob;
    }
    public void RequestSpawnPostion(Vector3 pos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(RpcTeleportPlayer), RpcTarget.All, pos);
    }

    [PunRPC]
    public void IsMafia()
    {
        Debug.Log("당신은 마피아입니다.");
        isMafia = true;
        _model.attackPower *= 2.5f;
    }

    [PunRPC]
    public void AssignJob(int jobId)
    {
        JobId myJob = (JobId)jobId;
        _model.AssignJob(myJob);
        PlayerJob = myJob;
    }

    [PunRPC]
    public void RpcTeleportPlayer(Vector3 pos)
    {
        pos.y += 1.5f;
        transform.position = pos;
    }
}
