using UnityEngine;

// 상호작용 가능한 모든 오브젝트는 이 인터페이스를 붙힌다.
public interface IInteractable
{    
    // 상호작용 시 일어날 로직
    public void OnInteract(GameObject player);

    // 상호 작용 가능한 거리를 반환하는 메서드임
    public float GetInteractionDistance();
}
