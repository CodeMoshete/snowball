using UnityEngine;

public class OnPlayerFrozenTrigger : MonoBehaviour
{
    public PlayerEntity HitPlayer { get; private set; }
    public bool DisableAfterUse;
    public CustomAction OnHit;
    private void Start()
    {
        Service.EventManager.AddListener(EventId.PlayerHit, OnPlayerHit);
    }

    private bool OnPlayerHit(object cookie)
    {
        HitPlayer = (PlayerEntity)cookie;

        if (DisableAfterUse)
        {
            Service.EventManager.RemoveListener(EventId.PlayerHit, OnPlayerHit);
        }

        if (OnHit != null)
        {
            OnHit.Initiate();
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.PlayerHit, OnPlayerHit);
    }
}
