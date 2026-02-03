using UnityEngine;

// 공격력 20% 증가
public class PoliceJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "경찰관";
        jobInformation = "공격력이 50% 증가합니다.";
        model.ApplyAttackMultiplier(1.5f);
    }
}
