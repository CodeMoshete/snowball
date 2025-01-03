using System.Collections.Generic;

public class MultiAction : CustomAction
{
    public List<CustomAction> NextActions;
    public MultiAction()
    {
        NextActions = new List<CustomAction>();
    }

    public override void Initiate()
    {
        for (int i = 0, count = NextActions.Count; i < count; ++i)
        {
            NextActions[i].Initiate();
        }
    }
}
