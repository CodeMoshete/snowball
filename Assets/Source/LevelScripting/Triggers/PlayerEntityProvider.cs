using UnityEngine;

public class PlayerEntityProvider : MonoBehaviour
{
    public virtual PlayerEntity GetPlayerEntity()
    {
        return null;
    }
}
