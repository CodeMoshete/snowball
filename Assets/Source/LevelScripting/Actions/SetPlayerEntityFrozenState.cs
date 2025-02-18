using UnityEngine;

public class SetPlayerEntityFrozenState : CustomNetworkAction
{
    public PlayerEntityProvider PlayerEntitySource;
    public bool IsFrozen;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = PlayerEntitySource.GetPlayerEntity();
        if (IsFrozen)
        {
            player.FreezePlayerFromScript();
        }
        else
        {
            Debug.Log("Unfreezing player");
            player.OnPlayerUnfrozenClientRpc();
        }

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
