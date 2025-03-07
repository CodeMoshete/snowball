using UnityEngine;

public class SpawnResourceEventData
{
    public string ResourcePath;
    public Vector3 Position;
    public Vector3 Rotation;
}

public class SpawnLocalResourceAction : CustomNetworkAction
{
    public string ResourcePath;
    public TransformProvider TargetTransform;
    public Vector3 Position;
    public Vector3 Rotation;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        SpawnResourceEventData eventData = new SpawnResourceEventData();
        eventData.ResourcePath = ResourcePath;
        if (TargetTransform != null)
        {
            eventData.Position = TargetTransform.transform.position;
            eventData.Rotation = TargetTransform.transform.eulerAngles;
        }
        else
        {
            eventData.Position = Position;
            eventData.Rotation = Rotation;
        }
        Service.EventManager.SendEvent(EventId.OnSpawnLocalGameObject, eventData);

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
