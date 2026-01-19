using System.Collections;
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
    
    // 스태미너가 이 값 이상 회복될 때까지 스프린트 잠금
    private float _staminaReenableToRun = 25f;
    private bool _isSprintLock;
    private bool _isRunning;
    public bool IsSprintLock => _isSprintLock;
    public bool CanSprint => !_isSprintLock && _curStamina > 0f;

    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value && !_isSprintLock;
    }

    public readonly string animNameOfMove = "MoveValue";
    public readonly string animNameOfRun = "Running";
    public readonly string animNameOfCrouch = "Crouching";
    public readonly string animNameOfJump = "Jumping";
    public readonly string animNameOfAttack = "Attack";
    public readonly string animNameOfKnockBack = "KnockBack";
    public readonly string animNameOfDeath = "Death";

    public Animator Animator { get; private set; }
    //public bool IsRunning { get; set; }
    public bool IsCrouching { get; set; }
    public bool IsGrounded { get; set; }
    public BaseJob MyJob { get; private set; }

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
            _curHealthPoint = 0f;
            Debug.Log("사망하였습니다.");
            StartCoroutine(DeathCor());
        }
    }

    IEnumerator DeathCor()
    {
        PlayerController controller = GetComponent<PlayerController>();
        controller.StateMachine.ChangeState(controller.StateDeath);
        this.Animator.SetBool(animNameOfDeath, true);
        AnimatorStateInfo info =
            Animator.GetCurrentAnimatorStateInfo(0);
        while (info.IsName(animNameOfDeath) && info.normalizedTime < 0.95f)
        {
            yield return null;
            info = Animator.GetCurrentAnimatorStateInfo(0);
        }
        PlayerManager.Instance.NoticeDeathPlayer(controller);
    }

    public void HealingHealthPoint(float amount)
    {
        _curHealthPoint = Mathf.Min(_maxHealthPoint, _curHealthPoint + amount);
    }

    // 스태미나의 회복과 감소 메서드
    public void ConsumeStamina(float amount)
    {
        // TODO : 스태미나가 0이 되면 달리기가 불가능해지고
        // 일정이상 채워야 다시 달릴 수 있다. (한 20 ~ 30 정도?)
        // 스테미너가 0이 되면 강제로 걷기
        if (amount <= 0f) return;

        // 탈진 상태
        if (_isSprintLock)
        {
            _curStamina = Mathf.Max(0f, _curStamina); // 음수 처리
            IsRunning = false;
            return;
        }

        float prev = _curStamina;
        _curStamina = Mathf.Max(0f, _curStamina - amount);

        Debug.Log($"스테미너 소모 : {prev:F1} -> {_curStamina:F1}");

        if(_curStamina <= 0f)
        {
            _curStamina = 0f;
            _isSprintLock = true;
            IsRunning = false;

            Debug.Log("스테미너 탈진 (스프린트 잠금)");
        }
    }

    public void RecoverStamina(float amount)
    {
        if (amount <= 0f) return;

        float prev = _curStamina;
        _curStamina = Mathf.Min(_maxStamina, _curStamina + amount);

        // 실수값 비교후 동일하지 않으면 Debug.Log
        if (!Mathf.Approximately(prev, _curStamina))
            Debug.Log($"스테미너 회복 : {prev:F1} -> {_curStamina:F1}");

        // 일정 이상 회복시 스프린트 잠금 해제
        if (_isSprintLock && _curStamina >= _staminaReenableToRun)
        {
            _isSprintLock = false;
            Debug.Log("스프린트 가능한 스테미너 회복");
        }
    }


    // 직업 배정 & 초기화 (직업 추가 시 여기에 계속 추가)
    public void AssignJob(JobId job)
    {
        switch (job)
        {
            case JobId.None:
                MyJob = null;
                break;
            case JobId.Doctor:
                MyJob = new DoctorJob();
                break;
            case JobId.Sprinter:
                MyJob = new SprinterJob();
                break;
        }
        MyJob?.Initialize(this);
    }
}
