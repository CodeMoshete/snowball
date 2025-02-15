public class SetBlackboardStringAction : CustomAction
{
    public string Key;
    public string Value;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        GameBlackboard.Instance.SetString(Key, Value);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
