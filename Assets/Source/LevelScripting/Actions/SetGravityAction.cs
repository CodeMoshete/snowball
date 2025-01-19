using UnityEngine;

public class SetGravityAction : CustomAction
{
    public float Gravity = -9.81f;
    public CustomAction NextAction;
    public override void Initiate()
    {
        Physics.gravity = new Vector3(0f, Gravity, 0f);
        
        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
