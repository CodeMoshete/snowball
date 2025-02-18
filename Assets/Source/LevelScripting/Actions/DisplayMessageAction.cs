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
        if (TargetPlayer == null || 
            NetworkManager.Singleton.LocalClientId == TargetPlayer.GetPlayerEntity().OwnerClientId)
        {
            Service.EventManager.SendEvent(EventId.DisplayMessage, Message);
            Service.TimerManager.CreateTimer(DisplayTime, DisplayTimeExpired, null);
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
