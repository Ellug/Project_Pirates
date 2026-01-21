// 직업에 각각 ID를 매칭함.
// 직업 무작위 배정 및 new 할당 Switch문으로 사용
public enum JobId
{
    None, Doctor, Sprinter, End
}

public abstract class BaseJob
{
    protected bool _isActive; // 액티브 스킬이라면 true (쿨타임 돌리기 위해)
    protected PlayerModel _model;

    public string name;

    // 직업 부여 후 초기화
    public virtual void Initialize(PlayerModel model)
    {
        _model = model;
    }

    // 직업의 고유 능력
    public virtual void UniqueSkill()
    {
        if (_model == null || _model.IsCrouching || !_model.IsGrounded) return;

        // 실제 로직 개별 클래스에서 적용
    }
}
