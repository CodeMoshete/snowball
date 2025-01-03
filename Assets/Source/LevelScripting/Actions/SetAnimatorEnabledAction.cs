using UnityEngine;

public class SetAnimatorEnabledAction : CustomAction
{
    public Animator Target;
    public bool IsEnabled;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        Target.enabled = IsEnabled;

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
