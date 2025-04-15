using UnityEngine;

public class CheckIfAnyPlayerInColliderAction : CustomNetworkAction
{
    public Collider ColliderToCheck;
    public CustomAction OnColliderOccupied;
    public CustomAction OnColliderEmpty;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {

        if (ColliderToCheck != null)
        {
            Collider[] colliders = Physics.OverlapBox(ColliderToCheck.bounds.center, ColliderToCheck.bounds.extents, Quaternion.identity);

            foreach (Collider collider in colliders)
            {
                PlayerEntity playerEntity = collider.GetComponent<PlayerEntity>();
                if (playerEntity != null)
                {
                    if (OnColliderOccupied != null)
                    {
                        OnColliderOccupied.Initiate();
                        return;
                    }
                }
            }
        }

        if (OnColliderEmpty != null)
        {
            OnColliderEmpty.Initiate();
        }
    }
}
