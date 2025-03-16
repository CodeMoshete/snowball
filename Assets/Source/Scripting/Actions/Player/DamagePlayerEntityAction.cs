using UnityEngine;

public class DamagePlayerEntityAction : CustomNetworkAction
{
    public PlayerEntityProvider PlayerEntitySource;
    public int DamageAmount;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = PlayerEntitySource.GetPlayerEntity();
        player.OnPlayerHit(DamageAmount);

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
