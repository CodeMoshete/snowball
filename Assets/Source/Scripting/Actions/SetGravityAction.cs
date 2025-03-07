using UnityEngine;

public class SetGravityAction : CustomNetworkAction
{
    public float Gravity = -9.81f;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Physics.gravity = new Vector3(0f, Gravity, 0f);
        
        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
