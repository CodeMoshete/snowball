public class RemoveEffectAction : CustomNetworkAction
{
    public EffectAction EffectToRemove;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (EffectToRemove != null)
        {
            EffectToRemove.RemoveEffect();
        }

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
