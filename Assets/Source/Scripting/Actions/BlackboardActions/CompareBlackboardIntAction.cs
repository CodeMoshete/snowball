public class CompareBlackboardIntAction : CustomNetworkAction
{
    public string Key;
    public int ComparisonValue;
    public CustomAction OnGreaterThan;
    public CustomAction OnLessThan;
    public CustomAction OnEqual;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        int value = GameBlackboard.Instance.GetInt(Key);

        if (value > ComparisonValue)
        {
            if (OnGreaterThan != null)
            {
                OnGreaterThan.Initiate();
            }
        }
        else if (value < ComparisonValue)
        {
            if (OnLessThan != null)
            {
                OnLessThan.Initiate();
            }
        }
        else
        {
            if (OnEqual != null)
            {
                OnEqual.Initiate();
            }
        }
    }
}
