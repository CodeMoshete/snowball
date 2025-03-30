public class IncrementBlackboardIntAction : CustomNetworkAction
{
    public string Key;
    public int IncrementBy;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        GameBlackboard.Instance.SetInt(Key, GameBlackboard.Instance.GetInt(Key) + IncrementBy);
        
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
