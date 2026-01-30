using ExitGames.Client.Photon;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using DG.Tweening;

public class DoorInteraction : InteractionObject
{
    [Header("Door Settings")]
    [SerializeField] private int _doorId;
    [SerializeField] private float _openSpeed = 5f;
    [SerializeField] private float _openAngle = 120f;

    [Header("Reference")]
    [SerializeField] private CustomPropertyManager _roomProps;

    [Tooltip("실제로 회전할 문 오브젝트")]
    [SerializeField] private Transform _doorPivot;

    [Header("Door SFX")]
    [SerializeField] private AudioClip _openClip;
    [SerializeField] private AudioClip _closeClip;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    private AudioSource _audio;
    private bool _lastOpenState;

    private bool _isOpen;
    private float _currentAngle;

    private Quaternion _closedRotation;

    private string DoorOpenKey => $"world.door.{_doorId}.open";
    private string DoorAngleKey => $"world.door.{_doorId}.angle";

    private void Awake()
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

    private void Start()
    {
        _closedRotation = _doorPivot.localRotation;
        DoorAction(_closedRotation);

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

    private void DoorAction(Quaternion targetRotation)
    {
        _doorPivot.DOLocalRotateQuaternion(targetRotation, 0.3f);
    }

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
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
            DoorAction(_closedRotation);
        else
            DoorAction(_closedRotation * Quaternion.Euler(0, angle, 0));

        if(!isInit && _lastOpenState != open)
        {
            PlayDoorSfx(open);
        }

        _lastOpenState = open;

        Debug.Log(isInit
            ? $"[Door] Init : {open} / {angle}"
            : $"[Door] Changed : {open} / {angle}");
    }

    private void PlayDoorSfx(bool isOpen)
    {
        if (_audio == null) return;

        AudioClip clip = isOpen ? _openClip : _closeClip;
        if (clip == null) return;

        _audio.PlayOneShot(clip, _sfxVolume);
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign Unique IDs")]
    private void AutoAssignIDs()
    {
        DoorInteraction[] foundObjects =
            FindObjectsByType<DoorInteraction>(FindObjectsSortMode.None);

        // 로컬 환경 통일을 위해 이름 순으로 정렬
        System.Array.Sort(foundObjects, (a, b) => string.Compare(a.name, b.name));

        // 찾아낸 오브젝트들에게 순차적으로 ID를 부여함
        for (int i = 0; i < foundObjects.Length; i++)
        {
            if (foundObjects[i]._doorId != i)
            {
                Undo.RecordObject(foundObjects[i], "Assign ID");
                foundObjects[i]._doorId = i;

                EditorUtility.SetDirty(foundObjects[i]);
            }
        }
        // 결과 확인용
        Debug.Log($"총 {foundObjects.Length}개의 오브젝트에 ID 할당이 완료되었습니다!");
    }
#endif
}
