using UnityEngine;
using Photon.Pun;
using System.Collections;

// 플레이어 간 상호작용 : 다른 사람에게 무언가 맞았을 때 실행될 로직 담당
public class PlayerHit : MonoBehaviour
{
    private PhotonView _view;
    private Rigidbody _rigidbody;
    private PlayerModel _model;

    void Awake()
    {
        _view = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _model = GetComponent<PlayerModel>();
    }

    // 다른 사람에게 밀려 충격을 받는 로직
    private void GetImpact(Vector3 dir, float force)
    {
        _rigidbody.AddForce(dir * force, ForceMode.Impulse);
    }

    private IEnumerator GetBondage(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    [PunRPC]
    public void RpcGetHitKnockBack(Vector3 dir, float force)
    {
        // 해당하는 사람이 아니면 실행할 필요 없음
        if (!_view.IsMine) return;

        GetImpact(dir, force);
    }

    [PunRPC]
    public void RpcGetHitAttack(float damage)
    {
        if (!_view.IsMine) return;

        _model.TakeDamage(damage);
    }

    [PunRPC]
    public void RpcGetHitBondage(float duration)
    {
        if (!_view.IsMine) return;

        // UI 표시 (당한 사람만)
        if (StatusNoticeUI.Instance != null)
        {
            StatusNoticeUI.Instance.ShowCountdown("로프에 속박됨", duration);
        }

        StartCoroutine(GetBondage(duration));
    }
}
