public class EffectAction : CustomNetworkAction
{
    public CustomAction StartAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (StartAction != null)
        {
            StartAction.Initiate();
        }
    }

    public void RemoveEffect()
    {
        Destroy(gameObject);
    }
}
