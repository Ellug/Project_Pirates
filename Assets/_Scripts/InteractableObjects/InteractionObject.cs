using UnityEngine;

// 상호작용 가능한 모든 오브젝트는 이 클래스를 상속 받는다.
public abstract class InteractionObject : MonoBehaviour
{
    // 모든 상호작용 오브젝트는 고유 ID를 가짐.
    [Header("Network Unique ID")]
    public int uniqueID = -1;

    [Header("Object Option")]
    [SerializeField] protected float _interactionDistance = 4f;

    // 상호작용을 한 사람에게 일어날 로직
    // (오버라이드 안하면 로그만 출력)
    public virtual void OnInteract(PlayerInteraction player)
    {
        Debug.Log("상호작용 됨.");
    }

    // 상호작용을 하지 않은 사람에게 일어날 로직
    // RPC로 수신 받을 때 호출한 메서드
    public virtual void OnOthersInteract()
    {
        Debug.Log($"누군가 {gameObject.name}을 상호작용 함.");
    }


    // 상호 작용 가능한 거리를 반환
    public float GetInteractionDistance()
    {
        return _interactionDistance;
    }

#if UNITY_EDITOR

    // Reset 메서드는 컴포넌트를 처음 붙힐 때 자동 실행
    // 또는 컴포넌트에 ... 눌러서 나오는 Reset을 눌러도 실행됨
    private void Reset()
    {
        // 자동으로 임의의 값 부여 (어차피 나중에 덮어씌움)
        uniqueID = GetInstanceID();
    }
#endif
}
