using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A basic example of client authoritative movement. It works in both client-server 
/// and distributed-authority scenarios.
/// </summary>
public class PlayerEntity : NetworkBehaviour
{
    public const float UNFREEZE_SECONDS = 5f;
    public const float UNFREEZE_DIST_THRESHOLD = 4f;
    public const string ICE_CUBE_RESOURCE = "IceCube";
    private readonly Color FROZEN_COLOR = new Color(0.33f , 0.33f, 0.33f, 0f);
    private readonly Color UNFROZEN_COLOR = new Color(1f , 0.61f, 0f, 0f);
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;
    public NetworkVariable<FixedString64Bytes> TeamName = new NetworkVariable<FixedString64Bytes>("Unassigned");
    public NetworkVariable<PlayerClass> CurrentPlayerClass = new NetworkVariable<PlayerClass>(global::PlayerClass.Soldier);
    public bool IsFrozen { get; private set; }
    private Transform iceCube;
    private Renderer iceCubeRenderer;
    private float frozenTimer;

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TeamName.OnValueChanged += OnTeamNameChanged;
        CurrentPlayerClass.OnValueChanged += OnPlayerClassChanged;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        gameManager.RegisterPlayer(OwnerClientId, this);
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
        AssignTeamNameServerRpc(startData.PlayerTeamName);
        AssignPlayerClassServerRpc(startData.PlayerClass);
        // gameManager.PlacePlayerAtSpawn(this);
        PlacePlayerAtSpawn(startData);
        Debug.Log("POS " + transform.position.ToString());
        Service.EventManager.RemoveListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
        return false;
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

    [ServerRpc]
    public void AssignPlayerClassServerRpc(PlayerClass playerClass)
    {
        CurrentPlayerClass.Value = playerClass;
        if (playerClass == PlayerClass.Queen)
        {
            Debug.Log($"Player {OwnerClientId} is the Queen!");
        }
    }

    private void OnPlayerClassChanged(PlayerClass oldValue, PlayerClass newValue)
    {
        Debug.Log($"Player class for player {OwnerClientId} changed to {newValue}");
    }

    private void PlacePlayerAtSpawn(GameStartData startData)
    {
        transform.position = startData.PlayerStartPos;
        transform.eulerAngles = startData.PlayerStartEuler;
        Rigidbody rigidBody = GetComponent<Rigidbody>();
        rigidBody.position = startData.PlayerStartPos;
        rigidBody.rotation = Quaternion.Euler(startData.PlayerStartEuler);
        // Debug.Log("Placed player " + OwnerClientId + " at " + startData.PlayerStartPos.ToString() + " <> " + transform.position.ToString());
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (IsFrozen)
        {
            Transform teamQueen = gameManager.GetQueenForTeam(TeamName.Value.ToString());
            if (teamQueen != null && Vector3.SqrMagnitude(transform.position - teamQueen.position) < UNFREEZE_DIST_THRESHOLD)
            {
                frozenTimer -= dt;
                float pct = 1f - (frozenTimer / UNFREEZE_SECONDS);
                Color unfreezeColor = Color.Lerp(FROZEN_COLOR, UNFROZEN_COLOR, pct);
                iceCubeRenderer.material.SetColor("_BaseColor", unfreezeColor);
                if (IsServer && frozenTimer <= 0f)
                {
                    OnPlayerUnfrozenClientRpc();
                }
            }
        }

        // IsOwner will also work in a distributed-authoritative scenario as the owner 
        // has the Authority to update the object.
        if (!IsOwner || !IsSpawned || IsFrozen) return;

        float multiplier = Speed * dt;
        float rotationMultiplier = RotationSpeed * dt;

        // Old input backends are enabled.
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * multiplier;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * multiplier;
        }

        if (Input.GetKey(KeyCode.W))
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

    public void OnPlayerFrozen()
    {
        iceCube = Instantiate(Resources.Load<GameObject>(ICE_CUBE_RESOURCE)).transform;
        iceCubeRenderer = iceCube.GetComponent<Renderer>();
        iceCube.position = transform.position;
        iceCube.rotation = transform.rotation;
        iceCube.SetParent(transform);
        IsFrozen = true;
        frozenTimer = UNFREEZE_SECONDS;
    }

    [ClientRpc]
    public void OnPlayerUnfrozenClientRpc()
    {
        iceCubeRenderer = null;
        Destroy(iceCube.gameObject);
        IsFrozen = false;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TeamName.OnValueChanged -= OnTeamNameChanged;
    }
}
