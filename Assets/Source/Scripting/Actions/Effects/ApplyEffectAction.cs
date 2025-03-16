using UnityEngine;

public class ApplyEffectAction : CustomNetworkAction
{
    public EffectAction EffectToApply;
    public Transform ObjectToApplyTo;
    public TransformProvider ObjectReferenceToApplyTo;
    public CustomAction OnComplete;
    
    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Transform applyTransform = null;
        if (ObjectToApplyTo != null)
        {
            applyTransform = ObjectToApplyTo;
        }
        else if (ObjectReferenceToApplyTo != null)
        {
            applyTransform = ObjectReferenceToApplyTo.GetTransform();
        }

        if (EffectToApply != null && applyTransform != null)
        {
            GameObject effectCloneObj = Instantiate<GameObject>(EffectToApply.gameObject, applyTransform);
            EffectAction effectClone = effectCloneObj.GetComponent<EffectAction>();
            if (effectClone != null)
            {
                effectClone.Initiate();
            }
        }

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
