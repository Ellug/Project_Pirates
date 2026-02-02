using UnityEngine;

// 능력 : 타인을 치료 (액티브형 능력)
public class DoctorJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        _isActive = true;
        base.Initialize(model);
        name = "의사";
        jobInformation = "현재는 아무 능력이 없다..";
        Debug.Log("의사 직업 배정 완료!");
    }

    public override void UniqueSkill()
    {
        Debug.Log("의사 직업 스킬 발동!");
    }
}
