using UnityEngine;

public class TestMission : MissionBase
{
    [ContextMenu("TEST / Complete")]
    public void TestComplete()
    {
        CompleteMission();
    }

    [ContextMenu("TEST / Fail")]
    public void TestFail()
    {
        FailMission();
    }
}
