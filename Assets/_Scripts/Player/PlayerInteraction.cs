using System.Collections;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _raycastDistance;
    [SerializeField] private LayerMask _interactableLayer;
    public bool IsInteractable { get; private set; }

    private GameObject _interactionBtn;
    private Camera _camera;
    private InteractionObject _curInteractable;
    private InteractionObjectRpcManager _rpcManager;

    private float _raycastInterval = 0.2f;
    private Coroutine _raycastRoutine;

    void Awake()
    {
        _camera = Camera.main;
        IsInteractable = false;
    }

    private void Start()
    {
        _interactionBtn = GameObject.Find("InteractionKey");
        _rpcManager = FindFirstObjectByType<InteractionObjectRpcManager>();

        if (_interactionBtn != null)
            _interactionBtn.SetActive(false);

        _raycastRoutine = StartCoroutine(RaycastRoutine());
    }

    private void OnDestroy()
    {
        StopCoroutine(_raycastRoutine);
    }

    private IEnumerator RaycastRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_raycastInterval);

        while (true) 
        {
            CheckInteractionObject();
            if (_interactionBtn != null)
                _interactionBtn.SetActive(IsInteractable);
            yield return wait;
        }
    }

    private void CheckInteractionObject()
    {
        // 레이캐스트로 쏴서 감지. 레이어 마스크 설정으로 부하 줄임
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _raycastDistance, _interactableLayer))
        {
            InteractionObject interactObj = hit.collider.GetComponent<InteractionObject>();

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
    // 오브젝트와 상호작용
    public void InteractObj()
    {
        if (_curInteractable != null)
        {
            // 상호작용 한 사람의 로직을 실행하고
            _curInteractable.OnInteract(this, _rpcManager);
        }
    }
}
