public class SetWallCostAction : CustomNetworkAction
{
    public int WallCost;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Constants.WallCost = WallCost;

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
