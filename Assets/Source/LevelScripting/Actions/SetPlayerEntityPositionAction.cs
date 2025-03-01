using UnityEngine;

public class SetPlayerEntityPositionAction : CustomNetworkAction
{
    public PlayerEntityProvider PlayerEntitySource;
    public Transform PositionReference;
    public TransformProvider PositionProvider;
    public Vector3 Position;
    public CustomAction OnDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = PlayerEntitySource.GetPlayerEntity();
        if (player == null)
        {
            Debug.LogWarning("PlayerEntityProvider.GetPlayerEntity() returned null");
            return;
        }

        if (PositionReference != null)
        {
            Position = PositionReference.position;
        }
        else if (PositionProvider != null)
        {
            Position = PositionProvider.GetTransformPosition();
            Position.y += 1.0f;
        }

        player.SetPlayerPositionFromScriptRpc(Position);
        Debug.Log($"Set player {player.name} position to {Position}");

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
