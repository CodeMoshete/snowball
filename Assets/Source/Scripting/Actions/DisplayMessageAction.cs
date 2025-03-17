using Unity.Netcode;
using UnityEngine;

public class DisplayMessageAction : CustomNetworkAction
{
    public string Message;
    public float DisplayTime;
    public PlayerEntityProvider TargetPlayer;
    public CustomAction OnComplete;
    public CustomAction OnMessageDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Debug.Log($"Display message: {Message}");
        if (TargetPlayer == null)
        {
            Service.EventManager.SendEvent(EventId.DisplayMessage, Message);
            Service.TimerManager.CreateTimer(DisplayTime, DisplayTimeExpired, null);
        }
        else
        {
            PlayerEntity playerEntity = TargetPlayer.GetPlayerEntity();
            if (playerEntity != null)
            {
                ulong clientId = playerEntity.OwnerClientId;
                BaseRpcTarget rpcParams = NetworkManager.Singleton.RpcTarget.Single(clientId, RpcTargetUse.Temp);
                playerEntity.DisplayMessageToPlayerRpc(Message, DisplayTime, rpcParams);
            }
        }
            
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }

    private void DisplayTimeExpired(object cookie)
    {
        Service.EventManager.SendEvent(EventId.HideMessage, null);
        if (OnMessageDone != null)
        {
            OnMessageDone.Initiate();
        }
    }
}
