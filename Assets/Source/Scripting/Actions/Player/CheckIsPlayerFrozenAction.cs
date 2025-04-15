using UnityEngine;

public class CheckIsPlayerFrozenAction : CustomNetworkAction
{
    public PlayerEntityProvider TargetPlayer;
    public CustomAction OnFrozen;
    public CustomAction OnNotFrozen;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (TargetPlayer == null)
        {
            Debug.LogError("[CheckIsPlayerFrozenAction] TargetPlayer is null!");
            return;
        }

        PlayerEntity playerEntity = TargetPlayer.GetPlayerEntity();
        if (playerEntity == null)
        {
            Debug.LogError("[CheckIsPlayerFrozenAction] PlayerEntity not found!");
            return;
        }

        if (playerEntity.IsFrozen)
        {
            OnFrozen?.Initiate();
        }
        else
        {
            OnNotFrozen?.Initiate();
        }

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
