using System.Collections;
using UnityEngine;

public class InteractableCCTV : InteractionObject
{
    [Header("CCTV UI")]
    [SerializeField] private GameObject _cctvCanvas;

    [Header("Player HUD")]
    [SerializeField] private GameObject _mainStatusCanvas; 

    private PlayerInteraction _player;
    private bool _isUsing = false;  

    public override void OnInteract(PlayerInteraction player, InteractionObjectRpcManager rpcManager)
    {
        if (_isUsing)
        {
            Debug.Log("이미 다른 플레이어가 사용 중입니다.");
            return;
        }
        _player = player;
        StartCoroutine(CCTVRoutine(rpcManager));
    }

    public override void OnOthersInteract()
    {
        _isUsing = true;
        Debug.Log("누군가 CCTV를 보고 있습니다.");
    }

    public void ResetUsingState()
    {
        _isUsing = false;
        Debug.Log("CCTV 사용을 마무리했습니다.");
    }

    IEnumerator CCTVRoutine(InteractionObjectRpcManager rpcManager)
    {
        _isUsing = true;

        rpcManager.RequestNetworkInteraction(uniqueID);

        ToggleUI(true);

        while (_isUsing)
        {
            if (_player.IsInteractable == false)
            {
                break;
            }
            yield return null;
        }

        ExitCCTV(rpcManager);
    }

    private void ExitCCTV(InteractionObjectRpcManager rpcManager)
    {
        _isUsing = false;
        rpcManager.RequestNetworkInteraction(uniqueID);
        ToggleUI(false);
    }

    private void ToggleUI(bool state)
    {
        if (_cctvCanvas != null) _cctvCanvas.SetActive(state);
        if (_mainStatusCanvas != null) _mainStatusCanvas.SetActive(!state);
    }
}
