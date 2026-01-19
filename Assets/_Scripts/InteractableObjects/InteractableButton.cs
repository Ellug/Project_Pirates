using UnityEngine;

public class InteractableButton : InteractionObject
{
    [SerializeField] private TestMission testMission;

    public override void OnInteract()
    {
        Debug.Log("내가 미션을 끝냈다.");
        testMission.TestComplete();
    }
    public override void OnOthersInteract()
    {
        Debug.Log("누군가 미션을 끝냈다.");
        testMission.TestComplete();
    }
}
