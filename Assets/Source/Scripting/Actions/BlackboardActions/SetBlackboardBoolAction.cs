public class SetBlackboardBoolAction : CustomNetworkAction
{
    public string Key;
    public bool Value;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        GameBlackboard.Instance.SetBool(Key, Value);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
