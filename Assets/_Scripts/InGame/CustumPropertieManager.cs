using ExitGames.Client.Photon;   
using Photon.Pun;               
using Photon.Realtime;           
using System;                   
using UnityEngine;               

public class CustumPropertieManager : MonoBehaviourPunCallbacks
{
    [Header("Debug View (Read Only)")]

    // 현재 룸 커스텀 프로퍼티에 등록된 것들을 인스펙터 상에서 확인
    [SerializeField] private Hashtable _cachedProperties = new();

    // 룸 커스텀 프로퍼티가 변경됐을 때 알리는 이벤트
    // → changedProps 에는 "바뀐 것만" 들어있음
    public event Action<Hashtable> OnRoomPropertyChanged;

    public void Set<T>(string key, T value)
    {
        // 방에 들어가 있지 않으면 실행하지 않음
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("CustumPropertieManager: Not in room");
            return;
        }

        // Photon에 넘길 Hashtable 생성 (변경할 값만 넣는다)
        var props = new Hashtable
        {
            { key, value }
        };

        // Room Custom Properties 네트워크 동기화
        // → 모든 클라이언트의 OnRoomPropertiesUpdate 호출됨
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public bool TryGet<T>(string key, out T value)
    {
        value = default;

        // 방이 아니면 실패
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        // 현재 방의 전체 커스텀 프로퍼티
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        // key 존재 + 타입 일치 여부 확인
        if (props != null && props.TryGetValue(key, out var v) && v is T cast)
        {
            value = cast;
            return true;
        }

        return false;
    }

    public bool HasKey(string key)
    {
        return PhotonNetwork.CurrentRoom?.CustomProperties?.ContainsKey(key) == true;
    }

    private void Start()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        //시작시 프로퍼티 캐시 초기화
        _cachedProperties.Clear();

        foreach (var pair in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            //현재 룸의 커스텀 프로퍼티 전부 불러와서 해쉬 테이블에 삽입
            _cachedProperties[pair.Key] = pair.Value;
        }

        // 변경점 콜백
        OnRoomPropertyChanged?.Invoke(_cachedProperties);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        // 변경된 값들을 로컬 캐시에 반영
        foreach (var key in changedProps.Keys)
        {
            _cachedProperties[key] = changedProps[key];
        }

        // 변경 알림 이벤트 호출
        OnRoomPropertyChanged?.Invoke(changedProps);
    }
}
