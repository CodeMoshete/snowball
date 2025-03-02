using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Throwables", order = 1)]
public class Throwables : ScriptableObject
{
    public List<ThrowableObject> ThrowableObjects;
}

[System.Serializable]
public struct ThrowableObject
{
    public SnowballType Type;
    public string PrefabName;
    public string PickupPrefabName;
    public string IconName;
    public string DisplayName;
    public string Description;
}
