using Unity.Netcode;

public class DeSpawnNetworkObjectAction : CustomNetworkAction
{
    public NetworkObject NetworkObject;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Service.EventManager.SendEvent(EventId.OnDeSpawnNetworkObject, NetworkObject.gameObject);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
