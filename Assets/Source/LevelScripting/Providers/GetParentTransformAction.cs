using UnityEngine;

public class GetParentTransformAction : TransformProvider
{
    public override Transform GetTransform()
    {
        return transform;
    }

    public override Vector3 GetTransformPosition()
    {
        return transform.position;
    }

    public override Vector3 GetTransformRotation()
    {
        return transform.eulerAngles;
    }
}
