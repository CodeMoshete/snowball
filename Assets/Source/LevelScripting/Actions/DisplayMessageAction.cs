using Unity.VisualScripting;
using UnityEngine;

public class DisplayMessageAction : CustomNetworkAction
{
    public string Message;
    public float DisplayTime;
    public CustomAction OnComplete;
    public CustomAction OnMessageDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Debug.Log($"Display message: {Message}");
        Service.EventManager.SendEvent(EventId.DisplayMessage, Message);
        Service.TimerManager.CreateTimer(DisplayTime, DisplayTimeExpired, null);
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
