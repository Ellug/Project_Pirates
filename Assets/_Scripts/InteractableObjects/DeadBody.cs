using System.Collections;
using UnityEngine;

public class DeadBody : InteractionObject
{
    private const float DEFAULT_REPORT_TIME = 2f;
    private PlayerInteraction _player;
    private Coroutine _reportCoroutine;
    private bool _reported = false;

    // PlayerManager에서 RPC로 생성 시 호출 (모든 클라이언트에서 동일 ID 사용)
    public void InitializeWithId(int id)
    {
        uniqueID = id;
        if (InteractionObjectRpcManager.Instance != null)
            InteractionObjectRpcManager.Instance.RegisterWithId(this, id);
    }

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        // 이미 신고 진행 중, 신고된 시체면 무시
        if (_reported) return;
        if (_reportCoroutine != null) return;

        _player = player;

        // 신고 시간 결정 (직업에 따라)
        float reportTime = GetReportTime(player);
        Debug.Log($"[DeadBody] 신고 시작 - 필요 시간: {reportTime}초");

        _reportCoroutine = StartCoroutine(ReportCountDown(reportTime));
    }

    public override void OnOthersInteract()
    {
        Debug.Log("[DeadBody] 시체 신고 RPC 수신");
    }

    IEnumerator ReportCountDown(float reportTime)
    {
        Debug.Log("시체 신고 시작");
        float timer = 0f;
        float second = 1f;

        while (timer <= reportTime)
        {
            timer += Time.deltaTime;

            // 계속 쳐다보고 있는지 검사
            if (!_player.IsInteractable)
            {
                Debug.Log("[DeadBody] 신고 취소 - 시선 이탈");
                _reportCoroutine = null;
                yield break;
            }

            if (timer >= second)
            {
                Debug.Log($"신고 진행 중 : {second}초");
                second += 1f;
            }
            yield return null;
        }

        _reported = true;
        Debug.Log("[DeadBody] 시체 신고 완료! 모든 플레이어 텔레포트 요청");

        // PlayerManager를 통해 모든 플레이어에게 텔레포트 RPC 전송
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.RequestTeleportAllPlayers();

        _reportCoroutine = null;
    }

    public float GetReportTime(PlayerInteraction player)
    {
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null) return DEFAULT_REPORT_TIME;

        BaseJob job = controller.GetPlayerJob();
        if (job == null) return DEFAULT_REPORT_TIME;

        return job.ReportTime;
    }
}
