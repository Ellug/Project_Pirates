// 직업에 각각 ID를 매칭함.
// 직업 무작위 배정 및 new 할당 Switch문으로 사용
public enum JobId
{
    None, 
    Delivery,
    SportMan,
    FireFighter,
    Reporter,
    Police,
    Wrestling,
    Thief,

    End
}

public abstract class BaseJob
{
    protected bool _isActive; // 액티브 스킬이라면 true (쿨타임 돌리기 위해)
    protected PlayerModel _model;

    public string name;
    public string jobInformation;

    // 시체 신고에 필요한 시간 (기본 2초, 특정 직업에서 오버라이드 하는 식으로 특성 구현)
    public virtual float ReportTime => 2f;

    // 직업 부여 후 초기화
    public virtual void Initialize(PlayerModel model)
    {
        _model = model;
    }

    // 직업의 고유 능력
    public virtual void UniqueSkill()
    {
        if (_model == null || _model.IsCrouching) return;

        // 실제 로직 개별 클래스에서 적용
    }
}
