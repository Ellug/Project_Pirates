using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseJob
{
    protected bool _isActive; // 액티브 스킬이라면 true (쿨타임 돌리기 위해)

    PlayerModel _model;

    // 직업 부여 후 초기화
    public virtual void Initialize(PlayerModel model)
    {
        _model = model;
        InputSystem.actions["JobSkill"].started += ctx => UniqueSkill();
    }

    // 직업의 고유 능력
    public abstract void UniqueSkill();
}
