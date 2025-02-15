public class SetBlackboardBoolAction : CustomAction
{
    public string Key;
    public bool Value;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        GameBlackboard.Instance.SetBool(Key, Value);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
