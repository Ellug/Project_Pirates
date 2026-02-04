using UnityEngine;

public class MemoryCell : MonoBehaviour
{
    public int _index;
    private MemoryMission _mission;

    public void Init(MemoryMission mission, int idx)
    {
        _mission = mission;
        _index = idx;
    }

    public void OnClick()
    {
        _mission?.OnCellClicked(_index);
    }
}
