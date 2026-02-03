// 스테미나 회복 속도 증가
public class WrestlingJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "씨름선수";
        jobInformation = "스테미나 회복 속도가 약간 빠릅니다.";
        model.StaminaRecoverPerSec *= 1.1f;
    }
}
