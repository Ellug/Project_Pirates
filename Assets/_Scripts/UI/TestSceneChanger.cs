using UnityEngine;
using Photon;
using Photon.Pun;

// 씬 전환 없이 PhotonNetwork 테스트 할 용도
public class TestSceneChanger : MonoBehaviourPunCallbacks
{
    //[SerializeField] private GameObject _playerPrefab;
    //public GameObject _myObj;

    private void Awake()
    {
        PhotonNetwork.ConnectUsingSettings(); // 포톤에 연결
    }

    public override void OnConnectedToMaster()
    {
        //PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        //_myObj = PhotonNetwork.Instantiate(_playerPrefab.name, transform.position, transform.rotation);
    }
}
