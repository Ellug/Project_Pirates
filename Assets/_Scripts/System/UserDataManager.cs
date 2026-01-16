using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;


[FirestoreData]
public sealed class UserDataManager : Singleton<UserDataManager>
{
    [Header("Runtime User Data")]
    [ReadOnly, SerializeField] private string _userId;
    [ReadOnly, SerializeField] private string _nickName;
    [ReadOnly, SerializeField] private int _win;
    [ReadOnly, SerializeField] private int _lose;

    public bool IsValidUser => !string.IsNullOrEmpty(_userId);
    public string UserId => _userId;
    public string NickName => _nickName;
    public int Win => _win;
    public int Lose => _lose;

    private FirebaseFirestore _db;
    private DocumentReference _userDoc;

    protected override void OnSingletonAwake()
    {
        _db = FirebaseFirestore.DefaultInstance;

        // ********************* 최종 빌드시 지우는거 고려하셈 *********************
        var settings = _db.Settings;
        settings.PersistenceEnabled = false;
    }

    // Firestore 조회 결과 반영 + 문서 참조 세팅
    public void ApplyFromFirestore(string userId, string nickName, int win, int lose)
    {
        _userId = userId;
        _nickName = nickName;
        _win = win;
        _lose = lose;

        _userDoc = _db.Collection("users").Document(_userId);
    }

    // 닉네임 변경 요청 (DB 성공 후에만 로컬 갱신)
    public async Task<bool> RequestNicknameChangeAsync(string newNick)
    {
        if (!IsValidUser || _userDoc == null) return false;
        if (string.IsNullOrWhiteSpace(newNick)) return false;

        try
        {
            await _userDoc.UpdateAsync("nickName", newNick);
            _nickName = newNick;
            Debug.Log("[UserDataManager] Nickname Update Complete." + newNick);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserData] Nickname update failed. userId={_userId}, newNick={newNick}\n{e}");
            return false;
        }
    }

    // 승리 +1 (서버는 원자적 증가, 성공 시에만 로컬 +1)
    public async Task<bool> AddWinAsync()
    {
        if (!IsValidUser || _userDoc == null) return false;

        try
        {
            await _userDoc.UpdateAsync("win", FieldValue.Increment(1));
            _win++;
            Debug.Log("[UserDataManager] Win Add Update Complete.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserData] Win update failed. userId={_userId}\n{e}");
            return false;
        }
    }

    // 패배 +1
    public async Task<bool> AddLoseAsync()
    {
        if (!IsValidUser || _userDoc == null) return false;

        try
        {
            await _userDoc.UpdateAsync("lose", FieldValue.Increment(1));
            _lose++;
            Debug.Log("[UserDataManager] Lose Add Update Complete.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserData] Lose update failed. userId={_userId}\n{e}");
            return false;
        }
    }

    public void Clear()
    {
        _userId = null;
        _nickName = null;
        _win = 0;
        _lose = 0;
        _userDoc = null;
        Debug.Log("[UserDataManager] User Data Cleared.");
    }
}
