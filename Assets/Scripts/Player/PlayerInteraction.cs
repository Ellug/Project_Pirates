using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _raycastDistance;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private GameObject interactionBtn;
    public bool IsInteractable { get; private set; }

    private Camera _camera;
    private IInteractable _curInteractable;
    private PhotonView _view;

    void Awake()
    {
        _camera = Camera.main;
        _view = GetComponent<PhotonView>();
        interactionBtn.SetActive(false);
        IsInteractable = false;
    }
    
    void Update()
    {
        CheckInteractionObject();
        interactionBtn.SetActive(IsInteractable);
    }

    private void CheckInteractionObject()
    {
        // 레이캐스트로 쏴서 감지. 레이어 마스크 설정으로 부하 줄임
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _raycastDistance, interactableLayer))
        {
            IInteractable interactObj = hit.collider.GetComponent<IInteractable>();

            if (interactObj != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);

                if (distance <= interactObj.GetInteractionDistance())
                {
                    IsInteractable = true;
                    if (_curInteractable != interactObj)
                        _curInteractable = interactObj;
                    return;
                }
            }
        }
        IsInteractable = false;
    }

    public void InteractObj()
    {
        if (_curInteractable != null)
            _view.RPC(nameof(TriggerObject), RpcTarget.All);
    }

    [PunRPC]
    private void TriggerObject()
    {
        _curInteractable.OnInteract(gameObject);
    }
}
