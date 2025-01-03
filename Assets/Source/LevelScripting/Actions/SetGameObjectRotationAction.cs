using UnityEngine;

public class SetGameObjectRotationAction : CustomAction
{
    public Transform Target;
    public Vector3 EulerAngles;
    public CustomAction OnDone;

    public override void Initiate()
    {
        Target.eulerAngles = EulerAngles;

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
