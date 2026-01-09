using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LightController : MonoBehaviourPunCallbacks
{
    [Header("조명 리스트")]
    [SerializeField] private List<Light> lightList = new List<Light>();

    [Header("조명 상태")]
    [SerializeField] private bool isLampsOn = true;

    [Header("조명 텀")]
    [SerializeField] private float rpcCooldown = 0.25f;
    private float lastRpcTime;

    void Start()
    {
        ApplyLightState(isLampsOn);
    }

    public void SwitchInteract()
    {
        if (!PhotonNetwork.InRoom)
        {
            SetLightState_Local(!isLampsOn);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            TrySetLightState(!isLampsOn);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestToggle), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_RequestToggle(PhotonMessageInfo info)
    {
        TrySetLightState(!isLampsOn);
    }

    private void TrySetLightState(bool newState)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isLampsOn == newState) return;
        if (Time.time - lastRpcTime < rpcCooldown) return;

        lastRpcTime = Time.time;
        photonView.RPC(nameof(RPC_SetLightState), RpcTarget.AllBuffered, newState);
    }

    [PunRPC]
    private void RPC_SetLightState(bool isOn)
    {
        isLampsOn = isOn;
        ApplyLightState(isOn);
    }

    private void SetLightState_Local(bool isOn)
    {
        isLampsOn = isOn;
        ApplyLightState(isOn);
    }

    private void ApplyLightState(bool isOn)
    {
        if (lightList == null || lightList.Count == 0) return;

        foreach (var l in lightList)
        {
            if (l != null)
                l.enabled = isOn;
        }
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SetLightState), RpcTarget.AllBuffered, isLampsOn);
        }
    }
}
