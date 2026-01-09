using UnityEngine;
using Photon.Pun;

public class InteractableButton : MonoBehaviour, IInteractable
{
    [SerializeField] private bool _isVictoryBtn;
    private PhotonView _view;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
    }

    public float GetInteractionDistance()
    {
        return 4f;
    }

    public void OnInteract(GameObject player)
    {
        _view.RPC(nameof(GameOverAndPopUpResult), RpcTarget.All);
    }

    [PunRPC]
    private void GameOverAndPopUpResult()
    {
        GameManager.Instance.GameOverAndResult(_isVictoryBtn);
    }
}
