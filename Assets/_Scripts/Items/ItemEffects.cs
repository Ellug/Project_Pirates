using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEffects
{
    public Dictionary<int, Action<PlayerModel>> itemEffectsDictionary = 
        new Dictionary<int, Action<PlayerModel>>();


    // 모든 아이템은 1회성이며 ID는 1부터 시작
    public void Initialize()
    {
        itemEffectsDictionary.Add(1, RecoveryHealthPoint);
        itemEffectsDictionary.Add(2, RecoveryStaminaPoint);
        itemEffectsDictionary.Add(3, (player) => player.StartCoroutine(TakeSpeedPill(player)));
        itemEffectsDictionary.Add(4, (player) => player.StartCoroutine(TakeAttackPill(player)));
        itemEffectsDictionary.Add(5, KnifeAttack);
        itemEffectsDictionary.Add(6, ThrowRope);
    }

    public void UseItem(int itemId, PlayerModel user)
    {
        if (itemEffectsDictionary.ContainsKey(itemId)) 
        {
            itemEffectsDictionary[itemId](user);
        }
        else
        {
            Debug.Log("사용할 수 없는 아이템입니다.");
        }
    }

    private void RecoveryHealthPoint(PlayerModel player)
    {
        Debug.Log("회복약 사용함");
        player.HealingHealthPoint(50f);
    }

    private void RecoveryStaminaPoint(PlayerModel player)
    {
        Debug.Log("스태미너 알약 사용함");
        player.RecoverStamina(50f);
    }
    
    IEnumerator TakeSpeedPill(PlayerModel player)
    {
        Debug.Log("달리기 속도 증가 알약 사용!");
        player.ChangeSpeedStatus(3f);
        yield return new WaitForSecondsRealtime(5f);
        player.ChangeSpeedStatus(-3f);
        Debug.Log("달리기 속도 정상화..");
    }

    IEnumerator TakeAttackPill(PlayerModel player)
    {
        Debug.Log("공격력 증가 알약 사용!");
        player.ChangeDamageStatus(10f);
        yield return new WaitForSecondsRealtime(5f);
        player.ChangeDamageStatus(-10f);
        Debug.Log("공격력 정상화..");
    }

    private void KnifeAttack(PlayerModel player)
    {
        // 다른 플레이어에게 큰 데미지를 줌
        RaycastHit hit;
        float knifeRange = 2f; // 공격 사거리가 주먹보다 조금 더 길다.
        float knifeDamage = 80f;

        if (player.OtherPlayerInteraction(out Vector3 direction, out hit, knifeRange))
        {
            Debug.Log("단검 명중 성공!");
            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                targetView.RPC("RpcGetHitAttack", targetView.Owner, knifeDamage);
            }
        }
        else
            Debug.Log("단검 명중 실패!");
    }

    private void ThrowRope(PlayerModel player)
    {
        // 다른 플레이어를 속박함.
        RaycastHit hit;
        float ropeRange = 3f; // 공격 사거리

        if (player.OtherPlayerInteraction(out Vector3 direction, out hit, ropeRange))
        {
            Debug.Log("로프 명중 성공!");
            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                targetView.RPC("RpcGetHitBondage", targetView.Owner, 5f);
            }
        }
        else
            Debug.Log("로프 명중 실패!");
    }
}
