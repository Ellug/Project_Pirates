using UnityEngine;
using Photon.Pun;

public class InteractableButton : MonoBehaviour, IInteractable
{
    [SerializeField] private bool _isVictoryBtn;

    public float GetInteractionDistance()
    {
        return 4f;
    }

    public void OnInteract(GameObject player)
    {
        GameManager.Instance.GameOverAndResult(_isVictoryBtn);
    }
}
