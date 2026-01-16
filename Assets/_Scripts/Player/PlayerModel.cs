using UnityEngine;

public class PlayerModel
{
    private float _healthPoint;
    private float _stamina;
    private bool _isPirate;
    private BaseJob _myJob;

    public PlayerModel()
    {
        _healthPoint = 100f;
        _stamina = 100f;
    }
}
