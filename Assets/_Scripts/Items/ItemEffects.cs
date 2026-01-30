using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEffects
{
    public Dictionary<int, Action<PlayerModel>> itemEffectsDictionary = 
        new Dictionary<int, Action<PlayerModel>>();


    // 소모품은 1부터 시작
    // 장비템은 1001부터 시작
    // 그 외 미션 아이템 등을 2001부터 시작
    public void Initialize()
    {
        itemEffectsDictionary.Add(1, RecoveryHealthPoint);
        itemEffectsDictionary.Add(2, RecoveryStaminaPoint);
        itemEffectsDictionary.Add(3, (player) => player.StartCoroutine(TakeSpeedPill(player)));
        itemEffectsDictionary.Add(1001, EquipWeapon);
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
        player.HealingStaminaPoint(50f);
    }
    
    IEnumerator TakeSpeedPill(PlayerModel player)
    {
        Debug.Log("이동 속도 증가 알약 사용!");
        player.ChangeSpeedStatus(0.5f);
        yield return new WaitForSecondsRealtime(5f);
        player.ChangeSpeedStatus(-0.5f);
        Debug.Log("이동 속도 정상화..");
    }

    private void EquipWeapon(PlayerModel player)
    {
        // TODO : 무기 장착 로직 추가
        Debug.Log("무기를 장착했습니다.");
    }
}
