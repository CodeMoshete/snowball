using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class SnowballSpawnActionData
{
    public SnowballType Type;
    public int SnowballsPerArea;
}

public class SpawnSnowballsAction : CustomAction
{
    public List<BoxCollider> SpawnAreas;
    // public int SnowballsPerArea;
    public List<SnowballSpawnActionData> SnowballSpawnData;
    public CustomAction NextAction;

    public override void Initiate()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        List<SnowballSpawnData> spawnPositions = new List<SnowballSpawnData>();
        for (int i = 0, count = SpawnAreas.Count; i < count; ++i)
        {
            BoxCollider volume = SpawnAreas[i];
            for (int j = 0; j < SnowballSpawnData.Count; ++j)
            {
                SnowballSpawnData spawnData = new SnowballSpawnData();
                spawnData.Type = SnowballSpawnData[j].Type;
                spawnData.SpawnPositions = new List<Vector3>();
                for (int k = 0; k < SnowballSpawnData[j].SnowballsPerArea; ++k)
                {
                    float xVal = Random.Range(volume.bounds.min.x, volume.bounds.max.x);
                    float yVal = Random.Range(volume.bounds.min.y, volume.bounds.max.y);
                    float zVal = Random.Range(volume.bounds.min.z, volume.bounds.max.z);
                    spawnData.SpawnPositions.Add(new Vector3(xVal, yVal, zVal));
                }
                spawnPositions.Add(spawnData);
            }
        }

        Service.EventManager.SendEvent(EventId.OnSnowballsSpawnedFromScript, spawnPositions);

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
