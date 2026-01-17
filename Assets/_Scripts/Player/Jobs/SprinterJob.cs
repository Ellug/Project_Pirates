using UnityEngine;

// 능력 : 기본 이동속도 30% 증가 (패시브형 능력)
public class SprinterJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        name = "육상 선수";
        model.baseSpeed *= 1.3f;
    }

    public override void UniqueSkill()
    {
        
    }
}
