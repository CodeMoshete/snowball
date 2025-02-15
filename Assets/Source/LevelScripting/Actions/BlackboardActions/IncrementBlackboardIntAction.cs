public class IncrementBlackboardIntAction : CustomAction
{
    public string Key;
    public int IncrementBy;
    public CustomAction OnComplete;
    public override void Initiate()
    {
        GameBlackboard.Instance.SetInt(Key, GameBlackboard.Instance.GetInt(Key) + IncrementBy);
        
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
