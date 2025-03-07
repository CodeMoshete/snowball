using System.Collections.Generic;

public class MultiAction : CustomNetworkAction
{
    public List<CustomAction> NextActions;
    public MultiAction()
    {
        NextActions = new List<CustomAction>();
    }

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        for (int i = 0, count = NextActions.Count; i < count; ++i)
        {
            NextActions[i].Initiate();
        }
    }
}
