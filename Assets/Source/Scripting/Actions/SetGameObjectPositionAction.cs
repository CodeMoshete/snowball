using UnityEngine;

public class SetGameObjectPositionAction : CustomNetworkAction
{
    public GameObject TargetGameObject;
    public GameObject PositionReference;
    public Vector3 Position;
    public CustomAction OnDone;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (PositionReference != null)
        {
            Position = PositionReference.transform.position;
        }
        TargetGameObject.transform.position = Position;

        if (OnDone != null)
        {
            OnDone.Initiate();
        }
    }
}
