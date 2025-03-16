using UnityEngine;

public class DestroyGameObjectAction : CustomNetworkAction
{
    public GameObject GameObjectToDestroy;
    public CustomAction NextAction;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        if (GameObjectToDestroy != null)
        {
            Destroy(GameObjectToDestroy);
        }

        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
