using Firebase;
using Firebase.Auth;
using System;
using System.Collections;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    public FirebaseAuth Auth { get; private set; }

    public void Initialize()
    {
        Auth = FirebaseAuth.DefaultInstance;
    }

    public IEnumerator Login(
        string email, 
        string password, 
        Action<FirebaseUser> onSuccess, 
        Action<string> onFail)
    {
        var task = Auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            onFail?.Invoke("아이디 또는 비밀번호가 틀렸습니다.");
            yield break;
        }

        onSuccess?.Invoke(task.Result.User);
    }

    public IEnumerator SignUp(
        string email,
        string password,
        Action<FirebaseUser> onSuccess,
        Action<FirebaseException> onFail)
    {
        Debug.Log("[Auth] Start Login");
        var task = Auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            var firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            onFail?.Invoke(firebaseEx);
            yield break;
        }

        onSuccess?.Invoke(task.Result.User);
    }

    public void Logout()
    {
        Auth.SignOut();
    }
}