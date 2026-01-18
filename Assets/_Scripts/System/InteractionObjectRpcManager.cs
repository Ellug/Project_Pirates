using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
// 이건 유니티 에디터에서만 유효함. (빌드할 때 에러 방지)
#if UNITY_EDITOR
using UnityEditor;
#endif

// 인게임에서 오브젝트들이 플레이어와 상호작용할 때 RPC를 쏴주는 클래스
public class InteractionObjectRpcManager : MonoBehaviourPun
{
    private Dictionary<int, InteractionObject> _objectCache = 
        new Dictionary<int, InteractionObject>();

    private PhotonView _view;

    // Awake가 맞는지 Start가 맞는지 테스트 필요
    void Awake()
    {
        // 인게임에 들어오면 모든 상호작용 오브젝트를 찾는다.
        InteractionObject[] allObjects = 
            FindObjectsByType<InteractionObject>(FindObjectsSortMode.None);
        
        // 상호작용 오브젝트를 딕셔너리에 모두 담는다.
        foreach (InteractionObject item in allObjects)
        {
            if (!_objectCache.ContainsKey(item.uniqueID))
                _objectCache.Add(item.uniqueID, item);
            else
                Debug.LogError("중복 ID 존재함!");
        }

        _view = GetComponent<PhotonView>();
    }

    public void RequestNetworkInteraction(int id)
    {
        _view.RPC(nameof(RpcInteractionObject), RpcTarget.Others, id);
    }

    [PunRPC]
    private void RpcInteractionObject(int id)
    {
        _objectCache[id].OnOthersInteract();
    }

#if UNITY_EDITOR
    // 자동으로 고유 ID 부여. (오브젝트 배치에 변화가 생기는 등)
    // 지금은 에디터 단계에서 실행하지만 차후 인게임에서
    // 오브젝트 생성도 랜덤이라면 빌드 단계에서 로딩 중에 해야할 듯
    [ContextMenu("Auto Assign Unique IDs")]
    private void AutoAssignIDs()
    {
        InteractionObject[] foundObjects =
            FindObjectsByType<InteractionObject>(FindObjectsSortMode.None);

        // 로컬 환경 통일을 위해 이름 순으로 정렬
        System.Array.Sort(foundObjects, (a, b) => string.Compare(a.name, b.name));

        // 찾아낸 오브젝트들에게 순차적으로 ID를 부여함
        for (int i = 0; i < foundObjects.Length; i++)
        {
            if (foundObjects[i].uniqueID != i)
            {
                Undo.RecordObject(foundObjects[i], "Assign ID");
                foundObjects[i].uniqueID = i;

                EditorUtility.SetDirty(foundObjects[i]);
            }
        }
        // 결과 확인용
        Debug.Log($"총 {foundObjects.Length}개의 오브젝트에 ID 할당이 완료되었습니다!");
    }
#endif
}
