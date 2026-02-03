using UnityEngine;

// 최대 체력 40% 증가
public class WrestlingJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "씨름선수";
        jobInformation = "최대 체력이 크게 증가합니다.";
        model.ApplyMaxHPMultiplier(1.4f);
    }
}
