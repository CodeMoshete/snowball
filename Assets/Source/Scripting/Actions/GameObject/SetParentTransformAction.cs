using UnityEngine;

public class SetParentTransformAction : CustomNetworkAction
{
    public TransformProvider ChildTransform;
    public TransformProvider ParentTransform;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (ChildTransform != null)
        {
            Transform targetParent = ParentTransform != null ? ParentTransform.GetTransform() : null;
            ChildTransform.GetTransform().SetParent(targetParent);
        }
        else
        {
            Debug.LogWarning("ParentTransform is null. Cannot set parent.");
        }
    }
}
