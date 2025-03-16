using UnityEngine;

public class PhysicsExplosionAction : CustomNetworkAction
{
    public float InnerRadius;
    public float OuterRadius;
    public float Force;

    public TransformProvider ExplosionPositionReference;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        // Apply Force to all rigidbodies withing range, where everything within the InnerRadius
        // gets the full force and everything within the OuterRadius gets the force linearly scaled down to 0.

        Vector3 explosionPosition = transform.position;
        if (ExplosionPositionReference != null && ExplosionPositionReference.GetTransform() != null)
        {
            explosionPosition = ExplosionPositionReference.GetTransformPosition();
        }

        // Geta list of all rigidbodies in the scene
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

        // Use square magnitude to avoid square root calculation for distance checks
        float innerRadiusSquared = InnerRadius * InnerRadius;
        float outerRadiusSquared = OuterRadius * OuterRadius;
        float sqrDistanceBetweenRadiuses = outerRadiusSquared - innerRadiusSquared;

        // Apply force to each rigidbody based on distance
        foreach (Rigidbody rb in rigidbodies)
        {
            // For now, physics explosions don't affect players because of their low mass setting.
            PlayerEntity player = rb.gameObject.GetComponent<PlayerEntity>();
            if (player != null)
                continue;

            Vector3 direction = rb.transform.position - explosionPosition;
            float distanceSquared = direction.sqrMagnitude;

            // If the rigidbody is within the inner radius, apply full force
            if (distanceSquared < innerRadiusSquared)
            {
                rb.AddForce(direction.normalized * Force, ForceMode.Impulse);
            }
            // If the rigidbody is within the outer radius, apply force scaled down linearly
            else if (distanceSquared < outerRadiusSquared)
            {
                // float distance = Mathf.Sqrt(distanceSquared);
                float distanceMult = distanceSquared - innerRadiusSquared / sqrDistanceBetweenRadiuses;
                float force = Force * (1 - distanceMult / (OuterRadius - InnerRadius));
                rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            }
        }

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
