using Unity.Netcode;
using UnityEngine;

public class OnObjectiveHitTrigger : MonoBehaviour
{
    public GameObject HitObject { get; private set; }
    public PlayerEntity ThrownPlayer { get; private set; }
    public CustomAction OnHit;
    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Service.EventManager.AddListener(EventId.PlayerHit, OnObjectiveHit);
        }
    }

    private bool OnObjectiveHit(object cookie)
    {
        ObjectiveHitData hitData = (ObjectiveHitData)cookie;
        ThrownPlayer = hitData.ThrowingPlayer;

        if (OnHit != null)
        {
            OnHit.Initiate();
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.PlayerHit, OnObjectiveHit);
    }
}
