using Unity.Netcode;

public class CustomNetworkAction : CustomAction
{
    /// <summary>
    /// If checked, this action will be triggered by the server for all players.
    /// If unchecked, this action will be triggered locally on the player's client only.
    /// </summary>
    public bool IsNetworked;
    
    /// <summary>
    /// If checked, this action will run automatically for any clients that join late.
    /// Useful to make sure actions like "Remove the ice wall" gets run for late joiners.
    /// "IsNetworked" must be checked for this condition to be applied.
    /// </summary>
    public bool IsSyncedOnJoin;
    public override void Initiate()
    {
        if (IsNetworked)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Service.EventManager.SendEvent(EventId.NetworkActionTriggered, this);
            }
        }
        else
        {
            InitiateFromNetwork();
        }
    }

    public virtual void InitiateFromNetwork()
    {
        // Intentionally empty.
    }
}
