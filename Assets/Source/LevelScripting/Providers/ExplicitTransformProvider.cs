using UnityEngine;

public class ExplicitTransformProvider : TransformProvider
{
    public Transform Transform;
    public Vector3 Position;
    public Vector3 Rotation;
    public override Vector3 GetTransformPosition()
    {
        if (Transform != null)
        {
            return Transform.position;
        }
        return Position;
    }

    public override Vector3 GetTransformRotation()
    {
        if (Transform != null)
        {
            return Transform.eulerAngles;
        }
        return Rotation;
    }
}
