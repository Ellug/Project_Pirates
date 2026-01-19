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
        _model.Animator.SetFloat(_model.animNameOfMove, 0f); 
    }
    public void FrameUpdate() 
    {
        if (_player.InputMove != Vector2.zero)
            _player.StateMachine.ChangeState(_player.StateMove);
        else if (_player.InputJump == true)
            _player.StateMachine.ChangeState(_player.StateJump);
        else if (_model.IsCrouching == false &&
            (_player.InputAttack == true || _player.InputKnockBack == true))
            _player.StateMachine.ChangeState(_player.StateAttack);
        else if (_model.IsCrouching == true)
            _player.StateMachine.ChangeState(_player.StateCrouch);
    }
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

    public void Enter() 
    { 
        Debug.Log("Move 상태 진입");
        if (_model.IsRunning)
            _model.Animator.SetFloat(_model.animNameOfMove, 1f);
        else if (_model.IsCrouching)
            _model.Animator.SetFloat(_model.animNameOfMove, 0.01f);
        else
            _model.Animator.SetFloat(_model.animNameOfMove, 0.5f);
    }
    public void FrameUpdate()
    {
        if (_player.InputMove == Vector2.zero)
            _player.StateMachine.ChangeState(_player.StateIdle);
        else if (_model.IsCrouching == false && 
            (_player.InputAttack == true || _player.InputKnockBack == true))
            _player.StateMachine.ChangeState(_player.StateAttack);
        else if (_player.InputJump == true)
            _player.StateMachine.ChangeState(_player.StateJump);
    }

    public void PhysicsUpdate() 
    {
        Vector2 input = _player.InputMove;
        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;

        if (_model.IsRunning && _model.IsSprintLock == false)
        {
            // 러닝 상태에서는 스태미너 감소
            _model.Animator.SetBool(_model.animNameOfRun, true);
            _model.ConsumeStamina(_model.SprintStaminaDrainPerSec * Time.fixedDeltaTime);

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
        
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() 
    { 
    }
    public void Exit()
    {
        _player.SetInitInput();
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
    public void FrameUpdate() 
    {
        if (_player.InputMove != Vector2.zero)
            _player.StateMachine.ChangeState(_player.StateMove);
        else if (_model.IsCrouching == false)
            _player.StateMachine.ChangeState(_player.StateIdle);

    }
    public void PhysicsUpdate() { }
    public void Exit() 
    {
        _model.Animator.SetBool(_model.animNameOfCrouch, false);
        _player.SetInitInput();
    }
}

// 어택과 밀치기는 이 상태가 됨.
public class AttackState : IPlayerState
{
    PlayerController _player;
    PlayerModel _model;
    bool _isAttack;

    public AttackState(PlayerController player)
    {
        _player = player;
        _model = _player.GetComponent<PlayerModel>();
    }

    public void Enter() 
    {
        Debug.Log("Attack 상태 진입");
        // _isAttack의 값을 들어오자마자 고정하여
        // 어택 또는 밀치기 둘 중 하나만 수행하도록 함
        if (_player.InputAttack == true)
            _isAttack = true;
        else
            _isAttack = false;

        // 애니메이션 일단 실행
        if (_isAttack)
            _model.Animator.SetTrigger(_model.animNameOfAttack);
        else
            _model.Animator.SetTrigger(_model.animNameOfKnockBack);

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
                if (_isAttack)
                    targetView.RPC("RpcGetHitAttack", targetView.Owner, 20f);
                else
                    targetView.RPC("RpcGetHitKnockBack", targetView.Owner, direction, _model.knockBackForce);
            }
        }
    }

    
    public void FrameUpdate() 
    {
        AnimatorStateInfo info = 
            _model.Animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 종료 직전(95%)에 상태 전환함
        if ((info.IsName(_model.animNameOfAttack) && info.normalizedTime >= 0.95f) ||
            (info.IsName(_model.animNameOfKnockBack) && info.normalizedTime >= 0.95f))
            _player.StateMachine.ChangeState(_player.StateIdle);

    }

    public void PhysicsUpdate() { }

    public void Exit() 
    {
        // 상태 탈출할 때 입력 값 다시 true로 원복
        _player.SetInitInput();
    }
}

public class DeathState : IPlayerState
{
    PlayerController _player;

    public DeathState(PlayerController player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("사망 상태 진입, 모든 키 입력을 무시함.");
    }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void Exit() { }
}