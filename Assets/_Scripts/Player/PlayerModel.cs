using Unity.Jobs;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public float mouseSensitivity;
    public float knockBackForce = 5f;
    public float baseSpeed;
    public float jumpPower;
    public float runSpeed;
    public float crouchSpeed;

    private float _healthPoint;
    private float _stamina;
    private BaseJob _myJob;

    public readonly string animNameOfMove = "MoveValue";
    public readonly string animNameOfRun = "Running";
    public readonly string animNameOfCrouch = "Crouching";
    public readonly string animNameOfJump = "Jumping";

    public Animator Animator { get; private set; }
    public bool IsRunning { get; set; }
    public bool IsCrouching { get; set; }
    public bool IsGrounded { get; set; }

    private void Awake()
    {
        _healthPoint = 100f;
        _stamina = 100f;
        IsGrounded = true;
        Animator = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        _healthPoint = Mathf.Max(0f, _healthPoint - damage);
    }

    public void ConsumeStamina(float amount)
    {
        _stamina = Mathf.Max(0f, _stamina - amount);
    }

    public void RecoverStamina(float amount)
    {
        _stamina = Mathf.Min(100f, _stamina + amount);
    }

    public void AssignJob(BaseJob job)
    {
        _myJob = job;
        job?.Initialize(this);
    }
}
