// 앉아서 이동속도 증가
public class ThiefJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        base.Initialize(model);
        name = "도둑";
        jobInformation = "앉아서 이동할 때 조금 더 빨라집니다.";

        model.crouchSpeed *= 1.3f;
    }
}
