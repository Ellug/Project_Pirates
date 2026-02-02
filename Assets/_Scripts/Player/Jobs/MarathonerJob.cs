using UnityEngine;

public class MarathonerJob : BaseJob
{
    public override void Initialize(PlayerModel model)
    {
        _isActive = false;
        base.Initialize(model);

        name = "마라토너";
        //model.MaxStamina *= 1.3f;
    }
}
