using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class RoomManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        GameManager.Instance.SetSceneState(SceneState.Room);
    }

    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("[Room] L pressed → Room Out");
            LeaveRoom();
        }
    }

    // Method
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    // CB
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}
