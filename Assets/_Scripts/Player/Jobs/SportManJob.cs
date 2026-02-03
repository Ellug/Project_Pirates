// 최대 스테미너 20% 증가
public class SportManJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);

        name = "운동선수";
        jobInformation = "최대 스테미너가 20% 증가합니다.";
        model.ApplyMaxStaminaMultiplier(1.2f);
    }
}
