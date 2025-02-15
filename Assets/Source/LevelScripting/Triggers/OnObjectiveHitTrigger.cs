using Unity.Netcode;
using UnityEngine;

public class OnObjectiveHitTrigger : MonoBehaviour
{
    public string TargetObjectiveName;
    public CustomAction OnHit;

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Service.EventManager.AddListener(EventId.ObjectiveHit, OnObjectiveHit);
        }
    }

    private bool OnObjectiveHit(object cookie)
    {
        ObjectiveHitData hitData = (ObjectiveHitData)cookie;
        Debug.Log($"[OnObjectiveHitTrigger] Hit Objective: {hitData.ObjectiveName} Target: {TargetObjectiveName}");

        if (hitData.ObjectiveName == TargetObjectiveName && OnHit != null)
        {
            OnHit.Initiate();
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.ObjectiveHit, OnObjectiveHit);
    }
}
