public class CompareBlackboardStringAction : CustomAction
{
    public string Key;
    public string ComparisonValue;
    public CustomAction OnEqual;
    public CustomAction OnNotEqual;

    public override void Initiate()
    {
        string value = GameBlackboard.Instance.GetString(Key);

        if (value == ComparisonValue)
        {
            if (OnEqual != null)
            {
                OnEqual.Initiate();
            }
        }
        else
        {
            if (OnNotEqual != null)
            {
                OnNotEqual.Initiate();
            }
        }
    }
}
