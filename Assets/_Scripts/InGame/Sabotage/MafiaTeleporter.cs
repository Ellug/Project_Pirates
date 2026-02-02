using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MafiaTeleporter : MonoBehaviour
{
    [Header("Teleport Buttons (마피아 전용)")]
    [SerializeField] private Button _teleportButton0;
    [SerializeField] private Button _teleportButton1;
    [SerializeField] private Button _teleportButton2;

    [Header("Teleport Destinations")]
    [SerializeField] private Transform _destination0;
    [SerializeField] private Transform _destination1;
    [SerializeField] private Transform _destination2;

    private void Start()
    {
        // 버튼 초기 비활성화
        SetButtonsActive(false);

        // 버튼 클릭 이벤트 등록
        if (_teleportButton0 != null)
            _teleportButton0.onClick.AddListener(() => TeleportLocalPlayer(0));
        if (_teleportButton1 != null)
            _teleportButton1.onClick.AddListener(() => TeleportLocalPlayer(1));
        if (_teleportButton2 != null)
            _teleportButton2.onClick.AddListener(() => TeleportLocalPlayer(2));
    }

    private void OnDestroy()
    {
        if (_teleportButton0 != null)
            _teleportButton0.onClick.RemoveAllListeners();
        if (_teleportButton1 != null)
            _teleportButton1.onClick.RemoveAllListeners();
        if (_teleportButton2 != null)
            _teleportButton2.onClick.RemoveAllListeners();
    }

    // 마피아 여부에 따라 버튼 활성화/비활성화
    // PlayerController.IsMafia RPC 이후 호출
    public void SetButtonsActive(bool active)
    {
        if (_teleportButton0 != null)
            _teleportButton0.gameObject.SetActive(active);
        if (_teleportButton1 != null)
            _teleportButton1.gameObject.SetActive(active);
        if (_teleportButton2 != null)
            _teleportButton2.gameObject.SetActive(active);
    }

    private void TeleportLocalPlayer(int index)
    {
        Transform destination = index switch
        {
            0 => _destination0,
            1 => _destination1,
            2 => _destination2,
            _ => null
        };

        if (destination == null)
        {
            Debug.LogWarning($"[MafiaTeleporter] 목적지 {index}가 설정되지 않음");
            return;
        }

        if (PlayerController.LocalInstancePlayer == null)
        {
            Debug.LogWarning("[MafiaTeleporter] 로컬 플레이어를 찾을 수 없음");
            return;
        }

        var playerController = PlayerController.LocalInstancePlayer.GetComponent<PlayerController>();

        // RPC로 모든 클라이언트에 위치 동기화 (PhotonTransformView 덮어쓰기 이슈 방지)
        playerController.photonView.RPC("RpcTeleportPlayer", RpcTarget.All, destination.position);
    }
}
