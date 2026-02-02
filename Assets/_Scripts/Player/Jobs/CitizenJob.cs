using UnityEngine;

// 신고 속도 증가, 최대 체력 감소
public class CitizenJob : BaseJob
{
    public override float ReportTime => 1.0f;

    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "시민";
        jobInformation = "신고정신이 투철한 모범시민입니다.";

        model.ApplyBaseSpeedMultiplier(1.25f);
        model.ApplyMaxHPMultiplier(0.75f);
    }
}
