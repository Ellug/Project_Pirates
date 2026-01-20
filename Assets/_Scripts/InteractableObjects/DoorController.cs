using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class DoorInteraction : InteractionObject
{
    [Header("Door Settings")]
    [SerializeField] private string _doorId = "A";
    [SerializeField] private float _openSpeed = 5f;
    [SerializeField] private float _openAngle = 120f;

    [Header("Reference")]
    [SerializeField] private CustumPropertieManager _roomProps;

    [Tooltip("실제로 회전할 문 오브젝트")]
    [SerializeField] private Transform _doorPivot;

    private bool _isOpen;
    private float _currentAngle;

    private Quaternion _closedRotation;
    private Quaternion _targetRotation;

    private string DoorOpenKey => $"world.door.{_doorId}.open";
    private string DoorAngleKey => $"world.door.{_doorId}.angle";

    private void Awake()
    {
        if (_roomProps == null)
            _roomProps = FindFirstObjectByType<CustumPropertieManager>();

        if (_doorPivot == null)
            _doorPivot = transform;
    }

    private void Start()
    {
        _closedRotation = _doorPivot.localRotation;
        _targetRotation = _closedRotation;

        _roomProps.OnRoomPropertyChanged += OnRoomPropertyChanged;

        if (_roomProps.TryGet(DoorOpenKey, out bool open))
            _isOpen = open;

        if (_roomProps.TryGet(DoorAngleKey, out float angle))
            _currentAngle = angle;

        ApplyDoor(_isOpen, _currentAngle, true);
    }

    private void OnDestroy()
    {
        if (_roomProps != null)
            _roomProps.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    private void Update()
    {
        _doorPivot.localRotation = Quaternion.Slerp(
            _doorPivot.localRotation,
            _targetRotation,
            Time.deltaTime * _openSpeed);
    }

    public override void OnInteract(PlayerInteraction player)
    {
        bool newOpen = !_isOpen;
        float angle = newOpen ? _openAngle : 0f;

        _roomProps.Set(DoorOpenKey, newOpen);
        _roomProps.Set(DoorAngleKey, angle);
    }

    private void OnRoomPropertyChanged(Hashtable changedProps)
    {
        bool dirty = false;

        if (changedProps.TryGetValue(DoorOpenKey, out var open))
        {
            _isOpen = (bool)open;
            dirty = true;
        }

        if (changedProps.TryGetValue(DoorAngleKey, out var angle))
        {
            _currentAngle = (float)angle;
            dirty = true;
        }

        if (dirty)
            ApplyDoor(_isOpen, _currentAngle, false);
    }

    private void ApplyDoor(bool open, float angle, bool isInit)
    {
        if (!open)
            _targetRotation = _closedRotation;
        else
            _targetRotation = _closedRotation * Quaternion.Euler(0, angle, 0);

        Debug.Log(isInit
            ? $"[Door] Init : {open} / {angle}"
            : $"[Door] Changed : {open} / {angle}");
    }
}
