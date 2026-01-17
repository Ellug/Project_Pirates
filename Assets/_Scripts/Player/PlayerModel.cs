using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public float mouseSensitivity;
    public float knockBackForce = 5f;
    public float baseSpeed;
    public float jumpPower;
    [HideInInspector] public float runSpeed;
    [HideInInspector] public float crouchSpeed;

    private float _maxHealthPoint;
    private float _curHealthPoint;
    private float _maxStamina;
    private float _curStamina;
    private BaseJob _myJob;

    public readonly string animNameOfMove = "MoveValue";
    public readonly string animNameOfRun = "Running";
    public readonly string animNameOfCrouch = "Crouching";
    public readonly string animNameOfJump = "Jumping";
    public readonly string animNameOfAttack = "Attack";
    public readonly string animNameOfKnockBack = "KnockBack";
    public readonly string animNameOfDeath = "Death";

    public Animator Animator { get; private set; }
    public bool IsRunning { get; set; }
    public bool IsCrouching { get; set; }
    public bool IsGrounded { get; set; }

    private void Awake()
    {
        _maxHealthPoint = 100f;
        _curHealthPoint = _maxHealthPoint;
        _maxStamina = 100f;
        _curStamina = _maxStamina;
        IsGrounded = true;
        runSpeed = baseSpeed * 1.6f;
        crouchSpeed = baseSpeed * 0.4f;
        Animator = GetComponent<Animator>();
    }

    // 체력의 회복과 감소 메서드
    public void TakeDamage(float damage)
    {
        _curHealthPoint -= damage;
        Debug.Log($"{damage}의 피해를 받았고 남은 체력은 {_curHealthPoint} 입니다.");

        if (_curHealthPoint <= 0f)
        {
            Debug.Log("사망하였습니다.");
            _curHealthPoint = 0f;
            // TODO : 여기에 사망 로직 추가
        }
    }

    public void HealingHealthPoint(float amount)
    {
        _curHealthPoint = Mathf.Min(_maxHealthPoint, _curHealthPoint + amount);
    }

    // 스태미나의 회복과 감소 메서드
    public void ConsumeStamina(float amount)
    {
        _curStamina -= amount;
        // TODO : 스태미나가 0이 되면 달리기가 불가능해지고
        // 일정이상 채워야 다시 달릴 수 있다. (한 20 ~ 30 정도?)
        if (_curStamina <= 0f)
        {

        }
    }

    public void RecoverStamina(float amount)
    {
        _curStamina = Mathf.Min(_maxStamina, _curStamina + amount);
    }


    // 직업 배정 & 초기화 (직업 추가 시 여기에 계속 추가)
    public void AssignJob(JobId job)
    {
        switch (job)
        {
            case JobId.None:
                _myJob = null;
                break;
            case JobId.Doctor:
                _myJob = new DoctorJob();
                break;
            case JobId.Sprinter:
                _myJob = new SprinterJob();
                break;
        }
        _myJob?.Initialize(this);
    }
}
