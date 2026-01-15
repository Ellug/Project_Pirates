using Photon.Pun;
using UnityEngine;
using static UnityEngine.UI.Image;

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
    PlayerContoller _player;

    public IdleState(PlayerContoller player)
    {
        _player = player;
    }

    public void Enter() 
    {
        Debug.Log("Idle 상태 진입");
        _player.Animator.SetFloat("MoveValue", 0f); 
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() { }
}

public class MoveState : IPlayerState
{
    PlayerContoller _player;

    public MoveState(PlayerContoller player)
    {
        _player = player;
    }

    public void Enter() { Debug.Log("Move 상태 진입"); }
    public void FrameUpdate() { }
    public void PhysicsUpdate() 
    {
        Vector2 input = _player.InputMove;
        Vector3 dir = new Vector3(input.x, 0f, input.y).normalized;

        if (_player.IsRunning)
        {
            _player.Animator.SetBool(_player.animNameOfRun, true);
            _player.Animator.SetFloat(_player.animNameOfMove, 1f);
            _player.transform.Translate(Time.fixedDeltaTime * _player.runSpeed * dir);
            return;
        }
        if (_player.IsCrouching)
        {
            _player.Animator.SetBool(_player.animNameOfCrouch, true);
            _player.Animator.SetFloat(_player.animNameOfMove, 0.01f);
            _player.transform.Translate(Time.fixedDeltaTime * _player.crouchSpeed * dir);
            return;
        }
        _player.Animator.SetFloat(_player.animNameOfMove, 0.5f);
        _player.transform.Translate(Time.fixedDeltaTime * _player.walkSpeed * dir);
    }
    public void Exit() 
    { 
        _player.Animator.SetFloat(_player.animNameOfMove, 0f); 
    }
}

public class JumpState : IPlayerState
{
    PlayerContoller _player;
    Rigidbody _playerRigidBody;

    public JumpState(PlayerContoller player)
    {
        _player = player;
        _playerRigidBody = _player.GetComponent<Rigidbody>();
    }

    public void Enter() 
    {
        Debug.Log("Jump 상태 진입");
        _playerRigidBody.AddForce(Vector3.up * _player.jumpPower, ForceMode.Impulse);
        _player.Animator.SetTrigger(_player.animNameOfJump);
        _player.IsGrounded = false;
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit()
    {
        _player.IsGrounded = true;
    }
}

public class CrouchState : IPlayerState
{
    PlayerContoller _player;

    public CrouchState(PlayerContoller player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("Crouch 상태 진입");
        _player.Animator.SetBool(_player.animNameOfCrouch, true);
    }
    public void FrameUpdate() { }
    public void PhysicsUpdate() { }
    public void Exit() 
    {
        _player.Animator.SetBool(_player.animNameOfCrouch, false);
    }
}

public class AttackState : IPlayerState
{
    PlayerContoller _player;

    public AttackState(PlayerContoller player)
    {
        _player = player;
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
            Debug.Log($"맞은 사람 : {hit.collider.name}");
            if (hit.transform == _player.transform) return;

            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                float force = 20f;

                targetView.RPC("RpcGetHitKnockBack", RpcTarget.Others, direction, force);
                Debug.Log($"RPC 쐈음 {targetView.ViewID}");
            }
        }
        else
        {
            Debug.Log("맞은 사람 없음");
        }

    }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void Exit() { }
}