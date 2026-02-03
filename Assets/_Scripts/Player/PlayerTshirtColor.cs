using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class PlayerTshirtColor : MonoBehaviourPunCallbacks
{
    private const string UPPER_COLOR_KEY = SetPlayerColor.UPPER_COLOR_KEY;

    [Header("상의(티셔츠)에 해당하는 Renderer들")]
    [SerializeField] private Renderer[] upperRenderers;

    [Header("색 순서 머티리얼 (enum 순서와 동일)")]
    [SerializeField] private Material[] upperMaterials;

    private PhotonView _pv;

    void Awake()
    {
        _pv = GetComponent<PhotonView>();

        if (_pv == null)
            Debug.LogError("[PlayerTshirtColor] PhotonView not found.");
    }

    void Start()
    {
        // 스폰 직후, 이미 값이 들어와 있는 경우 대비
        ApplyFromProperty(_pv.Owner);
    }

    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        if (_pv == null) return;
        if (_pv.Owner == null) return;
        if (target == null) return;

        // 이 캐릭터 주인의 프로퍼티가 아니면 무시
        if (_pv.Owner != target) return;

        // 상의 색상 키가 변경된 경우만 반응
        if (!changedProps.ContainsKey(UPPER_COLOR_KEY)) return;

        ApplyFromProperty(target);
    }

    private void ApplyFromProperty(Player player)
    {
        if (player == null) return;
        if (player.CustomProperties == null) return;

        if (!player.CustomProperties.TryGetValue(UPPER_COLOR_KEY, out object value))
            return;

        PlayerColorType type = (PlayerColorType)(int)value;
        ApplyUpper(type);
    }

    private void ApplyUpper(PlayerColorType type)
    {
        if (upperMaterials == null || upperMaterials.Length == 0)
        {
            Debug.LogWarning("[PlayerTshirtColor] upperMaterials empty");
            return;
        }

        int index = (int)type;

        if (index < 0 || index >= upperMaterials.Length)
            index = 0;

        Material mat = upperMaterials[index];

        if (upperRenderers == null) return;

        foreach (var r in upperRenderers)
        {
            if (r == null) continue;   
            r.material = mat;         
        }
    }
}
