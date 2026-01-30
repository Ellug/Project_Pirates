using Photon.Pun;
using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon; 

public class GlobalDoorLockController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CustomPropertyManager _roomProps;
    [SerializeField] private float lockDuration = 10f;

    private const string LOCK_KEY = "world.door.locked";

    private Coroutine _unlockTimerCoroutine;

    private void Awake()
    {
        if (_roomProps == null)
            _roomProps = FindFirstObjectByType<CustomPropertyManager>();
    }

    private void Start()
    {
        _roomProps.OnRoomPropertyChanged += OnRoomPropertyChanged;

        if (_roomProps.TryGet(LOCK_KEY, out object lockValue))
        {
            HandleLockProperty(lockValue);
        }
    }

    private void OnDestroy()
    {
        if (_roomProps != null)
            _roomProps.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    public void CloseAndLockAllDoors()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        double unlockTime = PhotonNetwork.Time + lockDuration;

        _roomProps.Set(LOCK_KEY, unlockTime);

        Debug.Log($"[GlobalDoorLock] 모든 문 잠금 요청 (해제 시간: {unlockTime})");
    }

    private void OnRoomPropertyChanged(ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.TryGetValue(LOCK_KEY, out var value))
        {
            HandleLockProperty(value);
        }
    }

    private void HandleLockProperty(object value)
    {
        if (_unlockTimerCoroutine != null)
        {
            StopCoroutine(_unlockTimerCoroutine);
            _unlockTimerCoroutine = null;
        }

        if (value is double unlockTime)
        {
            _unlockTimerCoroutine = StartCoroutine(LockTimerCoroutine(unlockTime));
        }
    }

    private IEnumerator LockTimerCoroutine(double unlockTime)
    {
        Debug.Log("[GlobalDoorLock] 잠금 타이머 시작");

        while (PhotonNetwork.Time < unlockTime)
        {
            yield return null;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            _roomProps.Set(LOCK_KEY, false);
        }

        Debug.Log("[GlobalDoorLock] 잠금 시간 만료 및 프로퍼티 갱신");
        _unlockTimerCoroutine = null;
    }
}