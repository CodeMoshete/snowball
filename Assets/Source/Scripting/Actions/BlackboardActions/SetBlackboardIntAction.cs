public class SetBlackboardIntAction : CustomNetworkAction
{
    public string Key;
    public int SetValue;
    public CustomAction OnComplete;
    
    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        GameBlackboard.Instance.SetInt(Key, SetValue);
        
        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
