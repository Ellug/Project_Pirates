using UnityEngine;

// 상호작용 가능한 모든 오브젝트는 이 인터페이스를 붙힌다.
public interface IInteractable
{
    public void ActiveInteractButton();
    public void OnInteract(GameObject player);
    public float GetInteractionDistance();
}
