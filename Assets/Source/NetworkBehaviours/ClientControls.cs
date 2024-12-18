using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
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
    public const string ICE_CUBE_RESOURCE = "IceCube";
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;
    public NetworkVariable<FixedString64Bytes> TeamName = new NetworkVariable<FixedString64Bytes>("Unassigned");
    public bool IsFrozen { get; private set; }

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TeamName.OnValueChanged += OnTeamNameChanged;
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
        GameStartData startData = (GameStartData)cookie;
        Debug.Log("Level Load Complete for " + OwnerClientId + " (" + startData.PlayerTeamName + "), " + IsOwner);
        // TeamName = startData.PlayerTeamName;
        AssignTeamNameServerRpc(startData.PlayerTeamName);
        gameManager.PlacePlayerAtSpawn(this);
        Debug.Log("POS " + transform.position.ToString());
        Service.EventManager.RemoveListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
        return false;
    }

    private void Update()
    {
        // IsOwner will also work in a distributed-authoritative scenario as the owner 
        // has the Authority to update the object.
        if (!IsOwner || !IsSpawned || IsFrozen) return;

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

    [ServerRpc]
    public void AssignTeamNameServerRpc(FixedString64Bytes teamName)
    {
        // Set the team name on the server
        TeamName.Value = teamName;
    }

    // Callback for when the TeamName changes
    private void OnTeamNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        Debug.Log($"Team name changed from {oldValue} to {newValue}");
        // Update UI or visuals to reflect the new team name
    }

    public void OnPlayerFrozen()
    {
        Transform iceCube = Instantiate(Resources.Load<GameObject>(ICE_CUBE_RESOURCE)).transform;
        iceCube.position = transform.position;
        iceCube.rotation = transform.rotation;
        iceCube.SetParent(transform);
        IsFrozen = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TeamName.OnValueChanged -= OnTeamNameChanged;
    }
}
