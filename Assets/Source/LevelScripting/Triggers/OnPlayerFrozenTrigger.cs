using Unity.Netcode;
using UnityEngine;

public class OnPlayerFrozenTrigger : MonoBehaviour
{
    public PlayerEntity HitPlayer { get; private set; }
    public PlayerEntity ThrownPlayer { get; private set; }
    public bool DisableAfterUse;
    public CustomAction OnHit;
    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Service.EventManager.AddListener(EventId.PlayerHit, OnPlayerHit);
        }
    }

    private bool OnPlayerHit(object cookie)
    {
        PlayerHitData hitData = (PlayerHitData)cookie;
        HitPlayer = hitData.HitPlayer;
        ThrownPlayer = hitData.ThrowingPlayer;

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
