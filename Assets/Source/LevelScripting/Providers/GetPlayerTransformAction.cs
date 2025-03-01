using UnityEngine;

public class GetPlayerTransformAction : TransformProvider
{
    public PlayerEntityProvider PlayerEntity;

    public override Transform GetTransform()
    {
        return PlayerEntity.GetPlayerEntity().transform;
    }

    public override Vector3 GetTransformPosition()
    {
        return PlayerEntity.GetPlayerEntity().transform.position;
    }

    public override Vector3 GetTransformRotation()
    {
        return PlayerEntity.GetPlayerEntity().transform.eulerAngles;
    }
}
