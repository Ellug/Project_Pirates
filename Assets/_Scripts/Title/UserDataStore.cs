using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[FirestoreData]
public class UserData
{
    [FirestoreProperty] public string uuid { get; set; }
    [FirestoreProperty] public string nickName { get; set; }
    [FirestoreProperty] public int win { get; set; }
    [FirestoreProperty] public int lose { get; set; }
}

    public class UserDataStore : MonoBehaviour
{
    private FirebaseFirestore _firestore;

    public void Initialize()
    {
        _firestore = FirebaseFirestore.DefaultInstance;
    }

    //아이디에 데이터값 만들기
    public IEnumerator CreateUserData(string uuid, string nick, Action onSuccess, Action<string> onFail)
    {
        Debug.Log("[Firestore] Start CreateData");
        var data = new Dictionary<string, object>
        {
            { "uuid", uuid },
            { "nickName", nick },
            { "win", 0 },
            { "lose", 0 }
        };

        var task = _firestore
            .Collection("users")
            .Document(uuid)
            .SetAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError(task.Exception);
            onFail?.Invoke("유저 데이터 저장에 실패했습니다.");
            yield break;
        }

        onSuccess?.Invoke();
    }

    //닉네임 중복 확인.
    public IEnumerator CheckNicknameDuplicate(string nickname, Action<bool> onResult, Action<string> onFail)
    {
        Debug.Log("[Firestore] Start Check nickName table");
        var task = _firestore
            .Collection("users")
            .WhereEqualTo("nickName", nickname)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            onFail?.Invoke("닉네임 중복 확인 중 오류가 발생했습니다.");
            yield break;
        }

        onResult?.Invoke(task.Result.Count > 0);
    }

    //해당 유저 닉네임 가져오기.
    public IEnumerator GetUserData(string uuid, Action<UserData> onSuccess, Action<string> onFail)
    {
        Debug.Log("[Firestore] Start users Data");
        var task = _firestore
            .Collection("users")
            .Document(uuid)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            onFail?.Invoke("유저 정보를 불러오지 못했습니다.");
            yield break;
        }

        var snapshot = task.Result;

        if (!snapshot.Exists)
        {
            onFail?.Invoke("유저 데이터가 존재하지 않습니다.\n새로운 데이터를 입력해주세요.");
            yield break;
        }

        var data = snapshot.ConvertTo<UserData>();
        onSuccess?.Invoke(data);
    }
}