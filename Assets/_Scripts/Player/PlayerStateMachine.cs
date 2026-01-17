using Photon.Pun;
using UnityEngine;

public interface IPlayerState
{
    public void Enter();
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
        if (CurrentState != newState)
        {
            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}

public class IdleState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;

    public IdleState(PlayerController player)
    {
        _player = player;
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter() 
    {
        Debug.Log("Idle 상태 진입");
        _model.Animator.SetFloat("MoveValue", 0f); 
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

public class MoveState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;

    public MoveState(PlayerController player)
    {
        _player = player;
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter() { Debug.Log("Move 상태 진입"); }
    public void FrameUpdate() { }
    public void PhysicsUpdate() 
    {
        Vector2 input = _player.InputMove;
        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;

        if (_model.IsRunning)
        {
            _model.Animator.SetBool(_model.animNameOfRun, true);
            _model.Animator.SetFloat(_model.animNameOfMove, 1f);
            _player.transform.Translate(Time.fixedDeltaTime * _model.runSpeed * dir);
            return;
        }
        if (_model.IsCrouching)
        {
            _model.Animator.SetBool(_model.animNameOfCrouch, true);
            _model.Animator.SetFloat(_model.animNameOfMove, 0.01f);
            _player.transform.Translate(Time.fixedDeltaTime * _model.crouchSpeed * dir);
            return;
        }
        _model.Animator.SetFloat(_model.animNameOfMove, 0.5f);
        _player.transform.Translate(Time.fixedDeltaTime * _model.baseSpeed * dir);
    }
    public void Exit() 
    {
        _model.Animator.SetFloat(_model.animNameOfMove, 0f); 
    }
}

public class JumpState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;
    Rigidbody _playerRigidBody;

    public JumpState(PlayerController player)
    {
        _player = player;
        _playerRigidBody = _player.GetComponent<Rigidbody>();
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter() 
    {
        Debug.Log("Jump 상태 진입");
        _playerRigidBody.AddForce(Vector3.up * _model.jumpPower, ForceMode.Impulse);
        _model.Animator.SetTrigger(_model.animNameOfJump);
        _model.IsGrounded = false;
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit()
    {
        _model.IsGrounded = true;
    }
}

public class CrouchState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;

    public CrouchState(PlayerController player)
    {
        _player = player;
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter()
    {
        Debug.Log("Crouch 상태 진입");
        _model.Animator.SetBool(_model.animNameOfCrouch, true);
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() 
    {
        _model.Animator.SetBool(_model.animNameOfCrouch, false);
    }
}

public class AttackState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;

    public AttackState(PlayerController player)
    {
        _player = player;
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter() 
    {
        Debug.Log("Attack 상태 진입");
        // 어택 상태에 들어오면 자신의 앞에 판정을 검사할 무언가를 만든다.
        float range = 1.5f; // 유효거리
        Vector3 direction = _player.transform.forward; // 바라보는 방향 (미는 방향)

        // 충돌 최적화를 위해 플레이어 레이어로 제한한다.
        int layerMask = 1 << LayerMask.NameToLayer("Player");

        RaycastHit hit;

        if (Physics.SphereCast(
            _player.transform.position, 0.5f, direction, out hit, range, layerMask))
        {
            if (hit.transform == _player.transform) return;

            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                targetView.RPC("RpcGetHitKnockBack", targetView.Owner, direction, _model.knockBackForce);
            }
        }
    }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void Exit() { }
}