using UnityEngine;

public class DamageEntitiesInRange : CustomNetworkAction
{
    public float InnerRadius;
    public float OuterRadius;
    public float DamageAmount;
    public PlayerEntityProvider ThrowingPlayer;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        // Use square magnitude to avoid square root calculation for distance checks
        float innerRadiusSquared = InnerRadius * InnerRadius;
        float outerRadiusSquared = OuterRadius * OuterRadius;
        float sqrDistanceBetweenRadiuses = outerRadiusSquared - innerRadiusSquared;

        DamagePlayerEntities(innerRadiusSquared, outerRadiusSquared, sqrDistanceBetweenRadiuses);
        DamageHealthObjects(innerRadiusSquared, outerRadiusSquared, sqrDistanceBetweenRadiuses);

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }

    private void DamagePlayerEntities(float innerRadiusSquared, float outerRadiusSquared, float sqrDistanceBetweenRadiuses)
    {
        // Get all entities in the scene
        PlayerEntity[] entities = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
        long throwingPlayerId = -1;
        if (ThrowingPlayer != null && ThrowingPlayer.GetPlayerEntity() != null)
        {
            throwingPlayerId = (long)ThrowingPlayer.GetPlayerEntity().OwnerClientId;
        }

        // Apply damage to each entity based on distance
        foreach (PlayerEntity player in entities)
        {
            Vector3 direction = player.transform.position - transform.position;
            float distanceSquared = direction.sqrMagnitude;

            // If the entity is within the inner radius, apply full damage
            if (distanceSquared < innerRadiusSquared)
            {
                player.DamagePlayerFromScript(DamageAmount, throwingPlayerId);
            }
            // If the entity is within the outer radius, apply damage scaled down linearly
            else if (distanceSquared < outerRadiusSquared)
            {
                float distanceMult = 1f - (distanceSquared - innerRadiusSquared) / sqrDistanceBetweenRadiuses;
                float damage = DamageAmount * distanceMult;
                player.DamagePlayerFromScript(damage, throwingPlayerId);
            }
        }
    }

    private void DamageHealthObjects(float innerRadiusSquared, float outerRadiusSquared, float sqrDistanceBetweenRadiuses)
    {
        // Get all entities in the scene
        ObjectHealthTrigger[] healthObjects = FindObjectsByType<ObjectHealthTrigger>(FindObjectsSortMode.None);

        // Apply damage to each entity based on distance
        foreach (ObjectHealthTrigger entity in healthObjects)
        {
            Vector3 direction = entity.transform.position - transform.position;
            float distanceSquared = direction.sqrMagnitude;

            // If the entity is within the inner radius, apply full damage
            if (distanceSquared < innerRadiusSquared)
            {
                int adjustedDamage = Mathf.RoundToInt((float)entity.Health / 100f * DamageAmount);
                entity.OnHit(adjustedDamage);
            }
            // If the entity is within the outer radius, apply damage scaled down linearly
            else if (distanceSquared < outerRadiusSquared)
            {
                float distanceMult = 1f - (distanceSquared - innerRadiusSquared) / sqrDistanceBetweenRadiuses;
                float damage = DamageAmount * distanceMult;
                int adjustedDamage = Mathf.RoundToInt(((float)entity.Health / 100f) * damage);
                Debug.Log($"Applying {adjustedDamage} damage to {entity.name}");
                entity.OnHit(adjustedDamage);
            }
        }
    }
}
