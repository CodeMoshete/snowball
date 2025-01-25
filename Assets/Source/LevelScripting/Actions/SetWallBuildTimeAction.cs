using UnityEngine;

public class SetWallBuildTimeAction : CustomNetworkAction
{
    public float BuildTime;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Constants.WallBuildTime = BuildTime;

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
