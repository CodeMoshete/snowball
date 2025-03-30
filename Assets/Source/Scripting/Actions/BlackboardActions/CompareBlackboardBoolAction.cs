public class CompareBlackboardBoolAction : CustomNetworkAction
{
    public string Key;
    public CustomAction OnTrue;
    public CustomAction OnFalse;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        bool value = GameBlackboard.Instance.GetBool(Key);
        if (value)
        {
            OnTrue.Initiate();
        }
        else
        {
            OnFalse.Initiate();
        }
    }
}
