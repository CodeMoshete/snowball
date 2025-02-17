using UnityEngine;

public class SetPlayerEntityPositionAction : CustomNetworkAction
{
    public PlayerEntityProvider PlayerEntitySource;
    public Transform PositionReference;
    public Vector3 Position;
    public CustomAction OnDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (PositionReference != null)
        {
            Position = PositionReference.position;
        }

        PlayerEntity player = PlayerEntitySource.GetPlayerEntity();
        player.transform.position = Position;

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
