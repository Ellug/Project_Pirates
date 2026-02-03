using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class HazardObject : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(0f)] private float _damage = 100f;

    [Header("Filter")]
    [SerializeField] private LayerMask _targetLayers = 1 << 7; // 기본 7번레이어 = 플레이어

    // 플레이어가 여러 콜라이더를 가진 경우 중복 타격 방지 (겹침 1회당 1번만)
    private readonly HashSet<int> _hitWhileOverlapping = new(64);

    private void OnCollisionEnter(Collision c) => TryHit(c.collider);
    private void OnCollisionExit(Collision c)  => ClearOverlap(c.collider);

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayers.value) == 0) return;

        var player = other.GetComponentInParent<PlayerModel>();
        if (player == null || player.isMafia) return;
        
        // 네트워크: 로컬 플레이어만 피해 처리 (원격 프록시에서 중복 사망 방지)
        var pv = player.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return;

        int id = player.transform.root.GetInstanceID();
        if (!_hitWhileOverlapping.Add(id)) return; // 이미 이번 겹침에서 맞았음

        player.TakeDamage(_damage);
    }

    private void ClearOverlap(Collider other)
    {
        var player = other.GetComponentInParent<PlayerModel>();
        if (player == null) return;

        int id = player.transform.root.GetInstanceID();
        _hitWhileOverlapping.Remove(id);
    }
}
