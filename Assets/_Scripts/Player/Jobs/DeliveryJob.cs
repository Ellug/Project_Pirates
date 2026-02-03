using UnityEngine;

// 능력 : 기본 이동속도 % 증가 (패시브형 능력)
public class DeliveryJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "배달기사";
        jobInformation = "이동속도가 10% 빠릅니다.";
        model.ApplyBaseSpeedMultiplier(1.1f);
    }
}
