using System.Collections.Generic;
using UnityEngine;

public class RandomSelectionAction : CustomNetworkAction
{
    public List<CustomAction> PossibleActions;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (PossibleActions != null)
        {
            int randomIndex = Random.Range(0, PossibleActions.Count);
            PossibleActions[randomIndex].Initiate();
        }

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
