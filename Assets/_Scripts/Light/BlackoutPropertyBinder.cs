using ExitGames.Client.Photon;
using UnityEngine;

public class BlackoutPropertyBinder : MonoBehaviour
{
    private const string BLACKOUT_KEY = "BLACKOUT";

    [Header("Reference")]
    [SerializeField] private CustomPropertyManager propertyManager;
    [SerializeField] private PowerSystem powerSystem;

    private void OnEnable()
    {
        propertyManager.OnRoomPropertyChanged += OnRoomPropertyChanged;
    }

    private void OnDisable()
    {
        propertyManager.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    private void Start()
    {
        if (propertyManager.TryGet(BLACKOUT_KEY, out bool isBlackout))
        {
            Apply(isBlackout);
        }
    }
    private void OnRoomPropertyChanged(Hashtable changedProps)
    {
        if (changedProps.TryGetValue(BLACKOUT_KEY, out var v) && v is bool isBlackout)
        {
            Apply(isBlackout);
        }
    }
    private void Apply(bool isBlackout)
    {
        if (isBlackout)
            powerSystem.PowerOff();
        else
            powerSystem.PowerOn();
    }
    public void RequestBlackout(bool on)
    {
        propertyManager.Set(BLACKOUT_KEY, on);
    }

    public bool TryGetBlackoutState(out bool isBlackout)
    {
        isBlackout = false;

        if (propertyManager == null)
            return false;

        return propertyManager.TryGet("BLACKOUT", out isBlackout);
    }
}
