using System.Collections;
using System;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public float mouseSensitivity;
    public float knockBackForce = 5f;
    public float baseSpeed;
    public float jumpPower;
    public float attackPower;
    [HideInInspector] public float runSpeed;
    [HideInInspector] public float crouchSpeed;

    private float _maxHealthPoint;
    public float MaxHP => _maxHealthPoint;

    private float _curHealthPoint;
    public float CurHP => _curHealthPoint;

    private float _maxStamina;
    public float MaxStamina => _maxStamina;

    private float _curStamina;
    public float CurStamina => _curStamina;

    [Header("Stamina")]
    [SerializeField] private const float _sprintStaminaDrainPerSec = 20f; // 소모
    public float SprintStaminaDrainPerSec => _sprintStaminaDrainPerSec;

    [SerializeField] private float _staminaRecoverPerSec = 20f; // 회복
    public float StaminaRecoverPerSec => _staminaRecoverPerSec;

    private float _staminaReenableToRun = 25f;
    private bool _isSprintLock;
    private bool _isRunning;
    public bool IsSprintLock => _isSprintLock;

    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value && !_isSprintLock;
    }

    private bool _isDead = false;
    public bool IsDead => _isDead;

    public readonly string animNameOfMove = "MoveValue";
    public readonly string animNameOfRun = "Running";
    public readonly string animNameOfCrouch = "Crouching";
    public readonly string animNameOfJump = "Jumping";
    public readonly string animNameOfAttack = "Attack";
    public readonly string animNameOfKnockBack = "KnockBack";
    public readonly string animNameOfDeath = "Death";

    public Animator Animator { get; private set; }
    public bool IsCrouching { get; set; }
    public BaseJob MyJob { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<ItemData[]> OnItemSlotChanged;

    private ItemData[] _inventory;
    private ItemEffects _effects;

    private void Awake()
    {
        _maxHealthPoint = 100f;
        _curHealthPoint = _maxHealthPoint;
        _maxStamina = 100f;
        _curStamina = _maxStamina;
        runSpeed = baseSpeed * 1.6f;
        crouchSpeed = baseSpeed * 0.4f;
        _inventory = new ItemData[2];
        Animator = GetComponent<Animator>();
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 15.0f);
        _isDead = false;
    }

    // 체력의 회복과 감소 메서드
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _curHealthPoint -= damage;
        Debug.Log($"{damage}의 피해를 받았고 남은 체력은 {_curHealthPoint} 입니다.");
        OnHealthChanged?.Invoke(_curHealthPoint, _maxHealthPoint);
        PostProcessingController.Instance.HitEffect();

        if (_curHealthPoint <= 0f)
        {
            _curHealthPoint = 0f;
            _isDead = true;
            Debug.Log("사망하였습니다.");
            StartCoroutine(DeathCor());
        }
    }

    public void HealingHealthPoint(float amount)
    {
        _curHealthPoint = Mathf.Min(_maxHealthPoint, _curHealthPoint + amount);
        OnHealthChanged?.Invoke(_curHealthPoint, _maxHealthPoint);
    }

    public void ExecuteByVote()
    {
        if (_isDead) return;

        _curHealthPoint = 0f;
        _isDead = true;
        OnHealthChanged?.Invoke(_curHealthPoint, _maxHealthPoint);

        Debug.Log("투표로 처형되었습니다.");
        StartCoroutine(DeathCor(vote: true));
    }

    IEnumerator DeathCor(bool vote = false)
    {
        PlayerController controller = GetComponent<PlayerController>();
        PlayerManager.Instance.NoticeDeathPlayer(controller);
        // 시체 생성 (네트워크 동기화)
        if (!vote) SpawnDeadBody();

        yield return null;

        controller.StateMachine.ChangeState(controller.StateDeath);
    }

    // 시체 생성되면서 사망 애니메이션 재생
    private void SpawnDeadBody()
    {
        transform.GetPositionAndRotation(out Vector3 spawnPos, out Quaternion spawnRot);

        // RPC로 모든 클라이언트에서 로컬 생성
        PlayerManager.Instance.RequestSpawnDeadBody(spawnPos, spawnRot);
    }

    public void ChangeSpeedStatus(float amount)
    {
        runSpeed += amount;
    }

    public void ChangeDamageStatus(float amount)
    {
        attackPower += amount;
    }

    // 스태미나의 회복과 감소 메서드
    public void ConsumeStamina(float amount)
    {
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
        OnStaminaChanged?.Invoke(_curStamina, _maxStamina);

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
            // Debug.Log($"스테미너 회복 : {prev:F1} -> {_curStamina:F1}");
            OnStaminaChanged?.Invoke(_curStamina + amount, _maxStamina);

        // 일정 이상 회복시 스프린트 잠금 해제
        if (_isSprintLock && _curStamina >= _staminaReenableToRun)
        {
            _isSprintLock = false;
            // Debug.Log("스프린트 가능한 스테미너 회복");
        }
    }


    // 직업 배정 & 초기화 (직업 추가 시 여기에 계속 추가)
    public void AssignJob(JobId job)
    {
        switch (job)
        {
            case JobId.None: MyJob = null; break;
            case JobId.Doctor: MyJob = new DoctorJob(); break;
            case JobId.Sprinter: MyJob = new SprinterJob(); break;
        }
        MyJob?.Initialize(this);
    }

    // 아이템 획득

    public bool TryGetItem(ItemData item)
    {
        for (int i = 0; i < _inventory.Length; i++)
        {
            if (_inventory[i] == null)
            {
                _inventory[i] = item;
                OnItemSlotChanged?.Invoke(_inventory);
                return true;
            }
        }
        return false;
    }

    // 아이템 사용

    public void RegistItemEffects(ItemEffects effects)
    {
        _effects = effects;
    }

    public bool TryUseItem(int index)
    {
        if (_inventory[index] != null)
        {
            if (_effects != null)
            {
                _effects.UseItem(_inventory[index].itemId, this);
                _inventory[index] = null;
                OnItemSlotChanged?.Invoke(_inventory);
                return true;
            }
        }
        return false;
    }

    // 다른 플레이어와 상호작용을 위해 자신의 앞을 판정한다.
    // 반환은 다른 플레이어가 감지되면 true 아니면 false, 기타 out으로 필요한 정보 할당.
    public bool OtherPlayerInteraction(out Vector3 direction, out RaycastHit raycastHit, float range = 1.5f)
    {
        direction = transform.forward; // 바라보는 방향

        // 충돌 최적화를 위해 플레이어 레이어로 제한한다.
        int layerMask = 1 << LayerMask.NameToLayer("Player");

        RaycastHit hit;

        if (Physics.SphereCast(
            transform.position, 0.5f, direction, out hit, range, layerMask))
        {
            raycastHit = hit;
            if (hit.transform == this.transform)
                return false;

            return true;
        }
        else
        {
            raycastHit = hit;
            return false;
        }
    }
}
