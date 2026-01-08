using UnityEngine;

public class InteractableButton : MonoBehaviour, IInteractable
{
    public void ActiveInteractButton()
    {

    }

    public float GetInteractionDistance()
    {
        return 4f;
    }

    public void OnInteract(GameObject player)
    {
        Debug.Log("상호작용 확인.");
    }
}
