using Photon.Pun;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GlobalDoorLockController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CustomPropertyManager _roomProps;
    [SerializeField] private float lockDuration = 10f;
    [SerializeField] private float lockCooldown = 20f;

    [Header("UI")]
    [SerializeField] private Button _doorLockButton;

    private const string LOCK_KEY = "world.door.locked";
    private const string LOCK_COOLDOWN_KEY = "world.door.locked.cooldown";

    private Coroutine _unlockTimerCoroutine;

    void Awake()
    {
        if (_roomProps == null)
            _roomProps = FindFirstObjectByType<CustomPropertyManager>();

        EnsureDoorLockButton();
        SetDoorLockButtonActive(false);
    }

    void Start()
    {
        _roomProps.OnRoomPropertyChanged += OnRoomPropertyChanged;

        if (_roomProps.TryGet(LOCK_KEY, out object lockValue))
        {
            HandleLockProperty(lockValue);
        }
    }

    void OnDestroy()
    {
        if (_roomProps != null)
            _roomProps.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    // 마피아 여부에 따라 버튼 활성화 (PlayerController.IsMafia에서 호출)
    public void SetDoorLockButtonActive(bool active)
    {
        EnsureDoorLockButton();
        if (_doorLockButton != null)
            _doorLockButton.gameObject.SetActive(active);
    }

    public void CloseAndLockAllDoors()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (_roomProps.TryGet(LOCK_COOLDOWN_KEY, out object cdValue))
        {
            if (cdValue is double cdEnd && PhotonNetwork.Time < cdEnd)
            {
                Debug.Log("[GlobalDoorLock] 쿨타임 중이라 요청 무시");
                return;
            }
        }

        double unlockTime = PhotonNetwork.Time + lockDuration;
        double cooldownEnd = PhotonNetwork.Time + lockCooldown;

        _roomProps.Set(LOCK_KEY, unlockTime);
        _roomProps.Set(LOCK_COOLDOWN_KEY, cooldownEnd);

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

    private void EnsureDoorLockButton()
    {
        if (_doorLockButton != null) return;

        // 비활성 오브젝트까지 찾기 위해 Resources API 사용
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var button in buttons)
        {
            if (button == null) continue;
            var scene = button.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) continue;
            if (button.gameObject.name == "Doorbtn")
            {
                _doorLockButton = button;
                break;
            }
        }

        if (_doorLockButton == null)
            Debug.LogWarning("[GlobalDoorLock] Doorbtn 버튼 참조를 찾지 못했습니다.");
    }
}
