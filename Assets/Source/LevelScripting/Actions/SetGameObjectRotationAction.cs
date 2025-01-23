using UnityEngine;

public class SetGameObjectRotationAction : CustomNetworkAction
{
    public Transform Target;
    public Vector3 EulerAngles;
    public CustomAction OnDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Target.eulerAngles = EulerAngles;

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
