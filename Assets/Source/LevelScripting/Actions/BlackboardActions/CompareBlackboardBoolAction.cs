public class CompareBlackboardBoolAction : CustomAction
{
    public string Key;
    public CustomAction OnTrue;
    public CustomAction OnFalse;

    public override void Initiate()
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
