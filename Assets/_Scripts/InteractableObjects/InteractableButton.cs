using UnityEngine;

public class InteractableButton : InteractionObject
{
    [SerializeField] private bool _isVictoryBtn;

    public override void OnInteract()
    {
        Debug.Log("내가 게임을 끝냈다");
        GameManager.Instance.GameOverAndResult(_isVictoryBtn);
    }
    public override void OnOthersInteract()
    {
        Debug.Log("누군가 게임을 끝냈다");
        GameManager.Instance.GameOverAndResult(_isVictoryBtn);
    }
}
