using UnityEngine;
using DG.Tweening;
using Photon.Pun;
using System.Collections;

public class DoorController : InteractionObject
{
    [Header("Door Settings")]
    public int doorId;
    [SerializeField] private float _openAngle = 120f;
    [SerializeField] private float _tweenDuration = 0.3f;

    [Header("Reference")]
    [SerializeField] private CustomPropertyManager _roomProps;

    [Tooltip("실제로 회전할 문 오브젝트")]
    [SerializeField] private Transform _doorPivot;

    [Header("Door SFX")]
    [SerializeField] private AudioClip _openClip;
    [SerializeField] private AudioClip _closeClip;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    private AudioSource _audio;

    private bool _isOpen;
    private bool _isLocked;
    private bool _lastOpenState;

    private float _currentAngle;
    private Quaternion _closedRotation;

    private Coroutine _lockTimerCoroutine;

    // Keys
    private string DoorOpenKey => $"world.door.{doorId}.open";
    private string DoorAngleKey => $"world.door.{doorId}.angle";
    private const string LOCK_KEY = "world.door.locked";

    void Awake()
    {
        if (_roomProps == null)
            _roomProps = FindFirstObjectByType<CustomPropertyManager>();

        if (_doorPivot == null)
            _doorPivot = transform;

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.playOnAwake = false;
        _audio.spatialBlend = 1f;
        _audio.minDistance = 1.5f;
        _audio.maxDistance = 15f;
    }

    void Start()
    {
        _closedRotation = _doorPivot.localRotation;

        _roomProps.OnRoomPropertyChanged += OnRoomPropertyChanged;

        SyncInitialState();
    }

    private void SyncInitialState()
    {
        if (_roomProps.TryGet(LOCK_KEY, out object lockVal))
        {
            HandleLockLogic(lockVal);
        }

        if (!_isLocked)
        {
            if (_roomProps.TryGet(DoorOpenKey, out bool open))
                _isOpen = open;

            if (_roomProps.TryGet(DoorAngleKey, out float angle))
                _currentAngle = angle;
        }

        ApplyDoor(_isOpen, _currentAngle, true);
    }

    void OnDestroy()
    {
        if (_roomProps != null)
            _roomProps.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    private void OnRoomPropertyChanged(ExitGames.Client.Photon.Hashtable changedProps)
    {
        bool dirty = false;

        if (changedProps.TryGetValue(LOCK_KEY, out var lockVal))
        {
            HandleLockLogic(lockVal);
            return;
        }

        if (!_isLocked)
        {
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

            if (dirty) ApplyDoor(_isOpen, _currentAngle, false);
        }
    }

    private void HandleLockLogic(object lockVal)
    {
        if (_lockTimerCoroutine != null) StopCoroutine(_lockTimerCoroutine);

        if (lockVal is double unlockTime)
        {
            _isLocked = true;
            _isOpen = false;
            _currentAngle = 0f;
            ApplyDoor(false, 0f, false);
            _lockTimerCoroutine = StartCoroutine(LockTimerCoroutine(unlockTime));
        }
        else if (lockVal is bool locked && !locked)
        {
            _isLocked = false;
        }
    }

    private IEnumerator LockTimerCoroutine(double unlockTime)
    {
        while (PhotonNetwork.Time < unlockTime)
        {
            yield return null;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            _roomProps.Set(LOCK_KEY, false);
        }

        _isLocked = false;
        _lockTimerCoroutine = null;
    }

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (_isLocked) return;

        bool newOpen = !_isOpen;
        float angle = newOpen ? _openAngle : 0f;

        _roomProps.Set(DoorOpenKey, newOpen);
        _roomProps.Set(DoorAngleKey, angle);
    }

    private void ApplyDoor(bool open, float angle, bool isInit)
    {
        Quaternion targetRot = open
            ? _closedRotation * Quaternion.Euler(0, angle, 0)
            : _closedRotation;

        _doorPivot.DOLocalRotateQuaternion(targetRot, _tweenDuration);

        if (!isInit && _lastOpenState != open)
            PlayDoorSfx(open);

        _lastOpenState = open;
    }

    private void PlayDoorSfx(bool isOpen)
    {
        if (_audio == null) return;
        AudioClip clip = isOpen ? _openClip : _closeClip;
        if (clip != null) _audio.PlayOneShot(clip, _sfxVolume);
    }
}