using UnityEngine;

public class UserDataManager : Singleton<UserDataManager>
{
    [Header("Check for UserData")]
    [ReadOnly, SerializeField] private string _userId;
    [ReadOnly, SerializeField] private string _nickName;
    [ReadOnly, SerializeField] private string _win;
    [ReadOnly, SerializeField] private string _lose;
}
