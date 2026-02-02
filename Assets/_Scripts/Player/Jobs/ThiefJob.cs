using UnityEngine;

// 이동속도 증가, 최대 체력 감소
public class ThiefJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);
        name = "도둑";
        jobInformation = "이동/회복 증가, 최대 체력이 감소합니다.";

        model.ApplyBaseSpeedMultiplier(1.20f);
        model.ApplyStaminaRecoverMultiplier(1.30f);
        model.ApplyMaxHPMultiplier(0.80f);
    }
}
