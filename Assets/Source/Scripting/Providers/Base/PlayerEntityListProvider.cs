using System.Collections.Generic;
using UnityEngine;

public class PlayerEntityListProvider : MonoBehaviour
{
    // Override this method!
    public virtual List<PlayerEntity> GetPlayerEntities()
    {
        return new List<PlayerEntity>();
    }
}
