using UnityEngine;

public interface IPlayerState
{
    public void Enter();
    public void HandleInput();
    public void FrameUpdate();
    public void PhysicsUpdate();
    public void Exit();
}

public class PlayerStateMachine
{
    public IPlayerState CurrentState { get; private set; }

    public PlayerStateMachine(IPlayerState initState)
    {
        CurrentState = initState;
        CurrentState.Enter();
    }

    public void ChangeState(IPlayerState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}

public class IdleState : IPlayerState
{
    public void Enter() { }
    public void HandleInput() { }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

public class MoveState : IPlayerState
{
    public void Enter() { }
    public void HandleInput() { }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

public class JumpState : IPlayerState
{
    public void Enter() { }
    public void HandleInput() { }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

public class CrouchState : IPlayerState
{
    public void Enter() { }
    public void HandleInput() { }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}