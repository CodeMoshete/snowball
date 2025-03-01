using UnityEngine;

public class TransformProvider : MonoBehaviour
{
    public virtual Transform GetTransform()
    {
        return null;
    }

    public virtual Vector3 GetTransformPosition()
    {
        return Vector3.zero;
    }

    public virtual Vector3 GetTransformRotation()
    {
        return Vector3.zero;
    }
}
