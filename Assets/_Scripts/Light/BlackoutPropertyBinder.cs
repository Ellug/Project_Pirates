using ExitGames.Client.Photon;
using UnityEngine;

public class BlackoutPropertyBinder : MonoBehaviour
{
    private const string BLACKOUT_KEY = "BLACKOUT";

    public bool IsBlackout { get; private set; }

    [Header("Reference")]
    [SerializeField] private CustomPropertyManager propertyManager;
    [SerializeField] private PowerSystem powerSystem;

    void OnEnable()
    {
        propertyManager.OnRoomPropertyChanged += OnRoomPropertyChanged;
    }

    void OnDisable()
    {
        propertyManager.OnRoomPropertyChanged -= OnRoomPropertyChanged;
    }

    void Start()
    {
        if (propertyManager.TryGet(BLACKOUT_KEY, out bool isBlackout))
        {
            Apply(isBlackout);
        }
        else
        {
            Apply(false);
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
        IsBlackout = isBlackout;

        if (isBlackout)
            powerSystem.PowerOff();
        else
            powerSystem.PowerOn();

        foreach (var light in FindObjectsByType<ProximityLight>(FindObjectsSortMode.None))
        {
            light.SetByPower(!isBlackout);
        }

        Debug.Log($"[Blackout] {(isBlackout ? "POWER OFF" : "POWER ON")}");
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

        return propertyManager.TryGet(BLACKOUT_KEY, out isBlackout);
    }
}
