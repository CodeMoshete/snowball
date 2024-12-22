using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PickupSystem
{
    private const float PICKUP_DISTANCE = 0.5f;
    private Dictionary<ulong, Transform> playerTransforms;
    private int numPickups;
    private int numPickupsToRemove;
    private List<Transform> pickups;
    private List<Transform> pickupsToRemove;
    private Action<Transform, PlayerEntity> onPickup;

    public PickupSystem(Dictionary<ulong, Transform> playerTransforms, Action<Transform, PlayerEntity> onPickup)
    {
        this.playerTransforms = playerTransforms;
        pickups = new List<Transform>();
        pickupsToRemove = new List<Transform>();
        this.onPickup = onPickup;
        Service.UpdateManager.AddObserver(OnUpdate);
    }

    public void RegisterPickup(Transform pickup)
    {
        numPickups++;
        pickups.Add(pickup);
    }

    public void UnregisterPickup(Transform pickup)
    {
        numPickupsToRemove++;
        pickupsToRemove.Add(pickup);
    }

    private void OnUpdate(float dt)
    {
        for (int i = 0; i < numPickups; ++i)
        {
            foreach(KeyValuePair<ulong, Transform> player in playerTransforms)
            {
                Transform pickup = pickups[i];
                if (Vector3.SqrMagnitude(player.Value.position - pickup.position) < PICKUP_DISTANCE)
                {
                    PlayerEntity playerEntity = player.Value.GetComponent<PlayerEntity>();
                    onPickup(pickup, playerEntity);
                }
            }
        }

        // We do this since it is expected pickups will be removed during their update above.
        if (numPickupsToRemove > 0)
        {
            for (int i = 0; i < numPickupsToRemove; ++i)
            {
                numPickups--;
                pickups.Remove(pickupsToRemove[i]);
            }
            pickupsToRemove.Clear();
            numPickupsToRemove = 0;
        }
    }

    public void OnDestroy()
    {
        Service.UpdateManager.RemoveObserver(OnUpdate);
        pickups = null;
        onPickup = null;
        playerTransforms = null;
    }
}
