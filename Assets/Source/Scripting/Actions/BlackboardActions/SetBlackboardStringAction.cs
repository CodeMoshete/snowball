public class SetBlackboardStringAction : CustomNetworkAction
{
    public string Key;
    public string Value;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        GameBlackboard.Instance.SetString(Key, Value);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
