using UnityEngine;

// 최대 체력 20% 증가
public class FireFighterJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "소방관";
        jobInformation = "최대 체력이 20% 증가합니다.";
        model.ApplyMaxHPMultiplier(1.2f);
    }
}
