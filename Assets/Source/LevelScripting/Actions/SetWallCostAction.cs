public class SetWallCostAction : CustomAction
{
    public int WallCost;
    public CustomAction NextAction;

    public override void Initiate()
    {
        Constants.WallCost = WallCost;

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
