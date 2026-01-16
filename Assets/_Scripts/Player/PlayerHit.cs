using UnityEngine;
using Photon.Pun;

// 플레이어 간 상호작용 : 다른 사람에게 무언가 맞았을 때 실행될 로직 담당
public class PlayerHit : MonoBehaviour
{
    private PhotonView _view;
    private Rigidbody _rigidbody;

    void Awake()
    {
        _view = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 다른 사람에게 밀려 충격을 받는 로직
    public void GetImpact(Vector3 dir, float force)
    {
        _rigidbody.AddForce(dir * force, ForceMode.Impulse);
        Debug.Log("충격 받음");
    }

    [PunRPC]
    public void RpcGetHitKnockBack(Vector3 dir, float force)
    {
        Debug.Log($"RPC 수신됨 {_view.ViewID}");
        // 해당하는 사람이 아니면 실행할 필요 없음
        if (!_view.IsMine) return;

        GetImpact(dir, force);
    }
}
