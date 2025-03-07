using Unity.Netcode;
using UnityEngine;

public class OnPlayerFrozenTrigger : PlayerEntityProvider
{
    public PlayerEntity HitPlayer { get; private set; }
    public PlayerEntity ThrownPlayer { get; private set; }
    public bool DisableAfterUse;
    public CustomAction OnHit;
    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Service.EventManager.AddListener(EventId.PlayerFrozen, OnPlayerFrozen);
        }
    }

    private bool OnPlayerFrozen(object cookie)
    {
        PlayerHitData hitData = (PlayerHitData)cookie;
        HitPlayer = hitData.HitPlayer;
        ThrownPlayer = hitData.ThrowingPlayer;

        if (DisableAfterUse)
        {
            Service.EventManager.RemoveListener(EventId.PlayerFrozen, OnPlayerFrozen);
        }

        if (OnHit != null)
        {
            OnHit.Initiate();
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.PlayerFrozen, OnPlayerFrozen);
    }

    public override PlayerEntity GetPlayerEntity()
    {
        return HitPlayer;
    }
}
