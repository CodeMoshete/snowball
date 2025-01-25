using UnityEngine;

public class SetAnimatorEnabledAction : CustomNetworkAction
{
    public Animator Target;
    public bool IsEnabled;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Target.enabled = IsEnabled;

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
