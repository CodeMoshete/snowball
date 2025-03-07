using System.Collections.Generic;
using UnityEngine;

public class SetGameObjectActiveAction : CustomNetworkAction
{
    public List<GameObject> Targets;
    public CustomAction OnDone;
    public bool SetActive;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        for (int i = 0, count = Targets.Count; i < count; ++i)
        {
            Targets[i].SetActive(SetActive);
        }

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
