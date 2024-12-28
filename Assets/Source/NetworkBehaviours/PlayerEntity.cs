using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
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
    public const string WALL_GHOST_RESOURCE = "WallSegmentGhost";
    public const string CROWN_ICON_RESOURCE = "CrownIcon";
    public const string CROWN_REFERENCE_POS_NAME = "CrownOrigin";
    private const string CAMERA_NAME = "Main Camera";
    private readonly Vector3 WALL_GHOST_PLAYER_OFFSET = new Vector3(0f, 0.75f, 2.5f);
    private readonly Vector3 WALL_GHOST_PLAYER_EULER = new Vector3(0f, 90f, 0f);
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;
    public NetworkVariable<FixedString64Bytes> TeamName = new NetworkVariable<FixedString64Bytes>(Constants.TEAM_UNASSIGNED);
    public NetworkVariable<PlayerClass> CurrentPlayerClass = new NetworkVariable<PlayerClass>(global::PlayerClass.Soldier);
    public NetworkVariable<int> SnowCount = new NetworkVariable<int>(3);

    public bool IsFrozen { get; private set; }
    public bool IsControlDisabled { 
        get
        {
            return IsFrozen || gameManager.CurrentGameState != GameState.Gameplay;
        }}
    private Transform iceCube;
    private Renderer iceCubeRenderer;
    private float frozenTimer;
    
    private bool isPlacingWall;
    private Transform ghostWall;

    private PlayerEntityControls controls;

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TeamName.OnValueChanged += OnTeamNameChanged;
        CurrentPlayerClass.OnValueChanged += OnPlayerClassChanged;
        SnowCount.OnValueChanged += OnSnowResourceChanged;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        gameManager.RegisterPlayer(OwnerClientId, this);
        if (IsOwner)
        {
            Debug.Log("Player OnNetworkSpawn - Setting up new player!");
            Service.EventManager.AddListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
            SetUpCamera();
        }
    }

    public void SetUpCamera()
    {
        GameObject cameraObj = GameObject.Find(CAMERA_NAME);
        if (cameraObj != null)
        {
            GameObject camOrigin = UnityUtils.FindGameObject(gameObject, "CameraOrigin");
            cameraObj.transform.SetParent(camOrigin.transform);
            cameraObj.transform.localPosition = Vector3.zero;
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
        PlacePlayerAtSpawn(startData);
        
        Transform teamQueen = gameManager.GetQueenForTeam(startData.PlayerTeamName);
        PlayerEntity player = teamQueen.GetComponent<PlayerEntity>();
        Debug.Log($"Enable crown for player {player.OwnerClientId}");
        player.ShowCrown();

        // Debug.Log("POS " + transform.position.ToString());
        Service.EventManager.RemoveListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);

        // Initialize player controls
        controls = new PlayerEntityControls(this);
        IControlScheme controlScheme = new KeyboardMouseControlScheme();
        controls.Initialize(controlScheme);
        return false;
    }

    [Rpc(SendTo.Server)]
    public void AssignTeamNameServerRpc(FixedString64Bytes teamName)
    {
        // Set the team name on the server
        TeamName.Value = teamName;
    }

    // Callback for when the TeamName changes
    private void OnTeamNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        // Update UI or visuals to reflect the new team name
        Debug.Log($"Team name changed from {oldValue} to {newValue}");
        gameManager.BroadcastRosterUpdate();
    }

    [Rpc(SendTo.Server)]
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

    [Rpc(SendTo.Server)]
    public void SetPlayerSnowCountServerRpc(int newValue)
    {
        SnowCount.Value = newValue;
    }

    private void OnSnowResourceChanged(int oldValue, int newValue)
    {
        Debug.Log($"Snow count for player {OwnerClientId} changed to {newValue} : {IsOwner}");
        if (IsOwner)
        {
            Service.EventManager.SendEvent(EventId.AmmoUpdated, newValue);
        }
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
            if (teamQueen != null)
            {
                PlayerEntity queenEntity = teamQueen.GetComponent<PlayerEntity>();
                if (!queenEntity.IsFrozen && Vector3.SqrMagnitude(transform.position - teamQueen.position) < UNFREEZE_DIST_THRESHOLD)
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
        }

        // IsOwner will also work in a distributed-authoritative scenario as the owner 
        // has the Authority to update the object.
        if (!IsOwner || !IsSpawned || IsFrozen) return;

        if (IsClient && isPlacingWall && Input.GetKeyDown(KeyCode.Escape))
        {
            Service.EventManager.SendEvent(EventId.WallPlacementEnd, null);
            isPlacingWall = false;
            Destroy(ghostWall.gameObject);
        }
    }

    public void OnThrowPressed()
    {
        if (IsClient && !isPlacingWall)
        {
            float loftPct = Mathf.Max(controls.CameraPitchPct - 0.4f, 0f) / 0.6f;

            gameManager.FireProjectileServerRpc(
                ProjectileOriginReference.position,
                ProjectileOriginReference.eulerAngles,
                ProjectileOriginReference.forward,
                loftPct,
                OwnerClientId
            );
        }
    }

    public void OnPlaceWallPressed()
    {
        if (!isPlacingWall)
        {
            StartPlacingWall();
        }
        else
        {
            isPlacingWall = false;
            Destroy(ghostWall.gameObject);
            gameManager.SpawnWallServerRpc(ghostWall.position, ghostWall.eulerAngles, OwnerClientId);
            Service.EventManager.SendEvent(EventId.WallPlacementEnd, null);
        }
    }

    private void StartPlacingWall()
    {
        if (SnowCount.Value < Constants.WALL_COST)
        {
            return;
        }

        Service.EventManager.SendEvent(EventId.WallPlacementBegin, null);
        isPlacingWall = true;
        ghostWall = Instantiate(Resources.Load<GameObject>(WALL_GHOST_RESOURCE)).transform;
        ghostWall.SetParent(transform);
        ghostWall.localPosition = WALL_GHOST_PLAYER_OFFSET;
        ghostWall.localEulerAngles = WALL_GHOST_PLAYER_EULER;
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

    public void ShowCrown()
    {
        Transform queenIcon = Instantiate(Resources.Load<GameObject>(CROWN_ICON_RESOURCE)).transform;
        queenIcon.SetParent(transform);
        Transform referencePos = UnityUtils.FindGameObject(gameObject, CROWN_REFERENCE_POS_NAME).transform;
        queenIcon.position = referencePos.position;
    }

    [Rpc(SendTo.Everyone)]
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
