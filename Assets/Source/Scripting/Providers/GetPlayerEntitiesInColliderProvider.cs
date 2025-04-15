using UnityEngine;
using System.Collections.Generic;

public class GetPlayerEntitiesInCollider : PlayerEntityListProvider
{
    public Collider ColliderToCheck;

    public override List<PlayerEntity> GetPlayerEntities()
    {
        List<PlayerEntity> playerEntities = new List<PlayerEntity>();

        if (ColliderToCheck != null)
        {
            Collider[] colliders = Physics.OverlapBox(ColliderToCheck.bounds.center, ColliderToCheck.bounds.extents, Quaternion.identity);

            foreach (Collider collider in colliders)
            {
                PlayerEntity playerEntity = collider.GetComponent<PlayerEntity>();
                if (playerEntity != null)
                {
                    playerEntities.Add(playerEntity);
                }
            }
        }

        return playerEntities;
    }
}
