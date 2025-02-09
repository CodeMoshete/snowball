using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnSnowballsAction : CustomAction
{
    public List<BoxCollider> SpawnAreas;
    public int SnowballsPerArea;
    public CustomAction NextAction;

    public override void Initiate()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        List<Vector3> spawnPositions = new List<Vector3>();
        for (int i = 0, count = SpawnAreas.Count; i < count; ++i)
        {
            BoxCollider volume = SpawnAreas[i];
            for (int j = 0; j < SnowballsPerArea; ++j)
            {
                float xVal = Random.Range(volume.bounds.min.x, volume.bounds.max.x);
                float yVal = Random.Range(volume.bounds.min.y, volume.bounds.max.y);
                float zVal = Random.Range(volume.bounds.min.z, volume.bounds.max.z);
                spawnPositions.Add(new Vector3(xVal, yVal, zVal));
            }
        }

        Service.EventManager.SendEvent(EventId.OnSnowballsSpawnedFromScript, spawnPositions.ToArray());

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
