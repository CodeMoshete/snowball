using UnityEngine;

public class SetGameObjectPositionAction : CustomAction
{
    public GameObject TargetGameObject;
    public GameObject PositionReference;
    public Vector3 Position;
    public CustomAction OnDone;

    public override void Initiate()
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
