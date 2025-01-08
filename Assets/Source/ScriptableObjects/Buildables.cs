using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Buildables", order = 1)]
public class Buildables : ScriptableObject
{
    public List<BuildableItem> BuildableItems;
}

[System.Serializable]
public struct BuildableItem
{
    public string PrefabName;
    public string GhostPrefabName;
    public Vector3 SpawnOffsetPos;
    public Vector3 SpawnOffsetEuler;
}
