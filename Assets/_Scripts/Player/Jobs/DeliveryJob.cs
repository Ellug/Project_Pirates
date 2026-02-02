using UnityEngine;

// 능력 : 기본 이동속도 % 증가 (패시브형 능력)
public class DeliveryJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        _isActive = false;
        base.Initialize(model);
        name = "배달기사";
        model.baseSpeed *= 1.15f;
    }
}
