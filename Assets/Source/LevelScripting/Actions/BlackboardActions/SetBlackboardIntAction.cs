public class SetBlackboardIntAction : CustomAction
{
    public string Key;
    public int SetValue;
    public CustomAction OnComplete;
    public override void Initiate()
    {
        GameBlackboard.Instance.SetInt(Key, SetValue);
        
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
