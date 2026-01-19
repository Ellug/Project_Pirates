using System.Collections;
using UnityEngine;

public class InteractableButton : InteractionObject
{
    [SerializeField] private TestMission testMission;

    private float _timer;
    private PlayerInteraction _player;

    public override void OnInteract(PlayerInteraction player)
    {
        _player = player;
        StartCoroutine(MissionCountDown());
    }

    public override void OnOthersInteract()
    {
        Debug.Log("누군가 미션 수행을 시도 중이다.");
    }

    IEnumerator MissionCountDown()
    {
        Debug.Log("미션 수행 시작");
        _timer = 0f;
        float second = 1f;
        while (_timer <= 10f) 
        {
            _timer += Time.deltaTime;
            if (_player.IsInteractable == false) // 계속 쳐다보고 있는지 검사
            {
                yield break;
            }
            if (_timer >= second)
            {
                Debug.Log($"미션 진행 중 : {second} / 10초");
                second += 1f;
            }
            yield return null;
        }
        Debug.Log($"미션 수행 완료!");
        testMission.TestComplete();
    }
}
