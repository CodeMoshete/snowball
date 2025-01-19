using UnityEngine;

public class SetWallBuildTimeAction : CustomAction
{
    public float BuildTime;
    public CustomAction NextAction;

    public override void Initiate()
    {
        Constants.WallBuildTime = BuildTime;

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
