public class SetBuildingAllowedAction : CustomNetworkAction
{
    public bool IsBuildingAllowed;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Constants.IsWallBuildingEnabled = IsBuildingAllowed;
        EventId eventToSend = IsBuildingAllowed ? EventId.OnWallBuildingEnabled : EventId.OnWallBuildingDisabled;
        Service.EventManager.SendEvent(eventToSend, null);

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
