using UnityEngine;

// 신고 시간 감소
public class ReporterJob : BaseJob
{
    public override float ReportTime => 0.2f;

    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "취재기자";
        jobInformation = "시체 신고 시간이 0.2초로 감소합니다.";
    }
}
