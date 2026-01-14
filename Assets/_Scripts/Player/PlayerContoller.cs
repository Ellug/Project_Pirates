using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerContoller : MonoBehaviourPunCallbacks
{
    public static GameObject LocalInstancePlayer;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _mouseSensitivity;

    private Vector2 _inputMove;
    private float _xRotation;
    private Camera _camera;
    private PhotonView _view;
    private PlayerInteraction _playerInteraction;
    private Animator _animator;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (!_view.IsMine) 
            return;

        _animator = GetComponent<Animator>();
        _playerInteraction = GetComponent<PlayerInteraction>();
        
        Cursor.lockState = CursorLockMode.Locked;
        
        _camera = Camera.main;
        _camera.transform.SetParent(transform, false);
        _camera.transform.localPosition = new Vector3(0f, 1.77f, 0f);
        
        _inputMove = Vector2.zero;

        InputSystem.actions["Move"].performed += OnMove;
        InputSystem.actions["Move"].canceled += OnMove;
        InputSystem.actions["Interact"].started += OnInteraction;
        InputSystem.actions["Look"].performed += OnLook;
    }

    void OnDestroy()
    {
        if (!_view.IsMine)
            return;

        Cursor.lockState = CursorLockMode.None;
        InputSystem.actions["Move"].performed -= OnMove;
        InputSystem.actions["Move"].canceled -= OnMove;
        InputSystem.actions["Interact"].started -= OnInteraction;
        InputSystem.actions["Look"].performed -= OnLook;
    }

    void FixedUpdate()
    {
        PlayerMove();
    }

    private void PlayerMove()
    {
        Vector3 dir = new Vector3(_inputMove.x, 0f, _inputMove.y).normalized;
        transform.Translate(Time.fixedDeltaTime * _moveSpeed * dir);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        _inputMove = ctx.ReadValue<Vector2>();
        if (ctx.performed)
        {
            _animator.SetFloat("MoveValue", 0.5f);
        }
        else
        {
            _animator.SetFloat("MoveValue", 0f);
        }
    }

    private void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (_playerInteraction.IsInteractable)
            _playerInteraction.InteractObj();
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        Vector2 mouseDelta = ctx.ReadValue<Vector2>();

        float mouseX = mouseDelta.x * _mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * _mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f); // 위아래 90도 제한

        // 카메라와 플레이어 몸체에 회전 적용
        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f); // 카메라만 위아래로
        transform.Rotate(Vector3.up * mouseX); // 플레이어 몸체 전체가 좌우로 회전
    }
}
