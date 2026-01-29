using UnityEngine;

public class BlackoutSwitchObject : InteractionObject
{
    [Header("Blackout Network Binder")]
    [SerializeField] private BlackoutPropertyBinder blackoutBinder;

    private const string BLACKOUT_KEY = "BLACKOUT";

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        base.OnInteract(player, rpcManager);

        if (blackoutBinder == null)
        {
            Debug.LogError("[BlackoutSwitchObject] BlackoutPropertyBinder not set.");
            return;
        }

        // 현재 상태 조회
        bool isBlackout = false;
        if (blackoutBinder.TryGetBlackoutState(out bool current))
        {
            isBlackout = current;
        }

        // 토글
        bool next = !isBlackout;

        Debug.Log($"[BlackoutSwitch] Toggle -> {next}");

        // 룸 프로퍼티에 상태 저장 (모든 클라이언트 동기화)
        blackoutBinder.RequestBlackout(next);
    }

    public override void OnOthersInteract()
    {
        base.OnOthersInteract();
        //사운드 연출 필요하면 여기서 처리 가능
    }
}
