using UnityEngine;

public class DisplayMessageAction : CustomAction
{
    public string Message;
    public float DisplayTime;
    public CustomAction OnComplete;
    public CustomAction OnMessageDone;

    public override void Initiate()
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
