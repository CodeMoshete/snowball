using NUnit.Framework;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A basic example of client authoritative movement. It works in both client-server 
/// and distributed-authority scenarios.
/// </summary>
public class ClientControls : NetworkBehaviour
{
    /// <summary>
    /// Movement Speed
    /// </summary>
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        gameManager.RegisterPlayer(OwnerClientId, transform);
        if (IsOwner)
        {
            Debug.Log("Player OnNetworkSpawn - Setting up new player!");
            Service.EventManager.AddListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
            gameManager.SetUpNewPlayer(transform);
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
    }

    private bool OnLevelLoadComplete(object cookie)
    {
        Debug.Log("Level Load Complete for " + OwnerClientId + ", " + IsOwner);
        gameManager.PlacePlayerAtSpawn(this);
        Debug.Log("POS " + transform.position.ToString());
        Service.EventManager.RemoveListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
        return false;
    }

    private void Update()
    {
        // IsOwner will also work in a distributed-authoritative scenario as the owner 
        // has the Authority to update the object.
        if (!IsOwner || !IsSpawned) return;

        float multiplier = Speed * Time.deltaTime;
        float rotationMultiplier = RotationSpeed * Time.deltaTime;

        // Old input backends are enabled.
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * multiplier;
        }
        else if(Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * multiplier;
        }
        
        if(Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * multiplier;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * multiplier;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(new Vector3(0f, -rotationMultiplier));
        }
        else if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(new Vector3(0f, rotationMultiplier));
        }

        if (IsClient && Input.GetKeyDown(KeyCode.Space))
        {
            gameManager.FireProjectileServerRpc(
                ProjectileOriginReference.position,
                ProjectileOriginReference.eulerAngles,
                ProjectileOriginReference.forward,
                OwnerClientId
            );
        }

        if (Input.GetKey(KeyCode.P))
        {
            transform.position = new Vector3(-10f, 0.5f, 0f);
        }
    }
}
