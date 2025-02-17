using Unity.Collections;
using Unity.Netcode;
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
    private const string BUILDABLE_RESOURCE_PATH = "BuildingPieces/";
    public const string WALL_GHOST_RESOURCE = "WallSegmentGhost";
    public const string CROWN_ICON_RESOURCE = "CrownIcon";
    public const string CROWN_REFERENCE_POS_NAME = "CrownOrigin";
    private const string CAMERA_NAME = "Main Camera";
    private const string BUILDABLES_RESOURCE = "BuildingPieces/WallBuildData";
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;
    public GameObject DefrostRangeFX;
    public NetworkVariable<FixedString64Bytes> TeamName = new NetworkVariable<FixedString64Bytes>(Constants.TEAM_UNASSIGNED);
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(Constants.PLAYER_NAME_DEFAULT);
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
    private Buildables wallOptions;
    private int currentWallOptionIndex;

    private PlayerEntityControls controls;

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        name = PlayerName.Value.ToString();
        TeamName.OnValueChanged += OnTeamNameChanged;
        PlayerName.OnValueChanged += OnPlayerNameChanged;
        CurrentPlayerClass.OnValueChanged += OnPlayerClassChanged;
        SnowCount.OnValueChanged += OnSnowResourceChanged;
        wallOptions = Resources.Load<Buildables>(BUILDABLES_RESOURCE);
        gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
        gameManager.RegisterPlayerTransform(this);
        if (IsOwner)
        {
            Debug.Log("Player OnNetworkSpawn - Setting up new player!");
            Service.EventManager.AddListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
            Service.EventManager.AddListener(EventId.OnWallBuildingDisabled, OnWallBuildingDisabled);
            SetUpCamera();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            GameObject cameraObj = GameObject.Find(CAMERA_NAME);
            if (cameraObj != null)
            {
                cameraObj.transform.SetParent(null);
            }

            // Controls will be null if this Entity is now owned by the local player.
            if (controls != null)
            {
                controls.Dispose();
            }
        }
        gameManager.DeregisterPlayer(this);
    }

    public void SetUpCamera()
    {
        GameObject cameraObj = GameObject.Find(CAMERA_NAME);
        if (cameraObj != null)
        {
            GameObject camOrigin = UnityUtils.FindGameObject(gameObject, "CameraArmature");
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
        AssignPlayerClassServerRpc(startData.PlayerClass);
        AssignTeamNameServerRpc(startData.PlayerTeamName);
        AssignPlayerNameRpc(startData.PlayerName);
        PlacePlayerAtSpawn(startData);
        
        Transform teamQueen = gameManager.GetQueenForTeam(startData.PlayerTeamName);
        PlayerEntity player = teamQueen.GetComponent<PlayerEntity>();
        Debug.Log($"Enable crown for player {player.OwnerClientId}");
        player.ShowCrown();

        // Debug.Log("POS " + transform.position.ToString());
        Service.EventManager.RemoveListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);

        // Initialize player controls
        controls = new PlayerEntityControls(this);
        IControlScheme controlScheme;
#if UNITY_STANDALONE_LINUX
        // Steam Deck
        controlScheme = new GamepadControlScheme();
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        controlScheme = new KeyboardMouseControlScheme();
#elif UNITY_ANDROID || UNITY_IOS
        controlScheme = new MobileControlScheme();
#endif
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
    public void AssignPlayerNameRpc(FixedString64Bytes playerName)
    {
        PlayerName.Value = playerName;
        name = playerName.ToString();
    }

    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        Debug.Log($"Player name changed from {oldValue} to {newValue}");
        name = newValue.ToString();
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
        if (newValue == PlayerClass.Queen && TeamName.Value.ToString() == gameManager.LocalPlayer.TeamName.Value.ToString())
        {
            ShowCrown();
        }
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
                    if (!DefrostRangeFX.activeSelf)
                        DefrostRangeFX.SetActive(true);

                    frozenTimer -= dt;
                    float pct = 1f - (frozenTimer / UNFREEZE_SECONDS);
                    Color unfreezeColor = Color.Lerp(FROZEN_COLOR, UNFROZEN_COLOR, pct);
                    iceCubeRenderer.material.SetColor("_BaseColor", unfreezeColor);
                    if (IsServer && frozenTimer <= 0f)
                    {
                        DefrostRangeFX.SetActive(false);
                        OnPlayerUnfrozenClientRpc();
                    }
                }
                else if (DefrostRangeFX.activeSelf)
                {
                    DefrostRangeFX.SetActive(false);
                }
            }
            else if (DefrostRangeFX.activeSelf)
            {
                DefrostRangeFX.SetActive(false);
            }
        }

        // IsOwner will also work in a distributed-authoritative scenario as the owner 
        // has the Authority to update the object.
        if (!IsOwner || !IsSpawned || IsFrozen) return;

        if (IsClient && isPlacingWall && Input.GetKeyDown(KeyCode.Escape))
        {
            Service.EventManager.SendEvent(EventId.OnWallPlacementEnded, null);
            Service.EventManager.SendEvent(EventId.HideMessage, null);
            isPlacingWall = false;
            Destroy(ghostWall.gameObject);
            ghostWall = null;
        }
    }

    public void OnEscapePressed()
    {
        if (isPlacingWall)
        {
            Service.EventManager.SendEvent(EventId.HideMessage, null);
            isPlacingWall = false;
            Destroy(ghostWall.gameObject);
        }
        else if (gameManager.CurrentGameState == GameState.Gameplay)
        {
            Service.EventManager.SendEvent(EventId.OnGamePause, null);
        }
        else if (gameManager.CurrentGameState == GameState.GameplayPaused)
        {
            Service.EventManager.SendEvent(EventId.OnGameResume, null);
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
        if (!Constants.IsWallBuildingEnabled)
            return;

        if (!isPlacingWall)
        {
            StartPlacingWall();
        }
        else
        {
            isPlacingWall = false;
            Service.EventManager.SendEvent(EventId.HideMessage, null);
            Service.EventManager.SendEvent(EventId.OnWallBuildStageStarted, Constants.WallBuildTime);
            Service.TimerManager.CreateTimer(Constants.WallBuildTime, FinishWallPlacement, null);
        }
    }

    private void FinishWallPlacement(object cookie)
    {
        Destroy(ghostWall.gameObject);
        Service.EventManager.SendEvent(EventId.OnWallPlacementEnded, null);
        BuildableItem currentItem = wallOptions.BuildableItems[currentWallOptionIndex];
        string resourceName = $"{BUILDABLE_RESOURCE_PATH}{currentItem.PrefabName}";
        gameManager.SpawnWallServerRpc(resourceName, ghostWall.position, ghostWall.eulerAngles, OwnerClientId);
        ghostWall = null;
    }

    private void DisplayBuildableOptionAtIndex(int index)
    {
        if (!isPlacingWall)
            return;

        if (ghostWall != null)
        {
            Destroy(ghostWall.gameObject);
            ghostWall = null;
        }

        BuildableItem nextItem = wallOptions.BuildableItems[index];
        ghostWall = Instantiate(Resources.Load<GameObject>($"{BUILDABLE_RESOURCE_PATH}{nextItem.GhostPrefabName}")).transform;
        ghostWall.SetParent(transform);
        ghostWall.localPosition = nextItem.SpawnOffsetPos;
        ghostWall.localEulerAngles = nextItem.SpawnOffsetEuler;
    }

    public void OnNextWallPressed()
    {
        if (!isPlacingWall)
            return;

        currentWallOptionIndex = 
            currentWallOptionIndex >= wallOptions.BuildableItems.Count - 1 ? 0 : currentWallOptionIndex + 1;
        DisplayBuildableOptionAtIndex(currentWallOptionIndex);
    }

    public void OnPrevWallPressed()
    {
        if (!isPlacingWall)
            return;

        currentWallOptionIndex = 
            currentWallOptionIndex <= 0 ? wallOptions.BuildableItems.Count - 1 : currentWallOptionIndex - 1;
        DisplayBuildableOptionAtIndex(currentWallOptionIndex);
    }

    private void StartPlacingWall()
    {
        if (SnowCount.Value < Constants.WallCost)
        {
            return;
        }

        Service.EventManager.SendEvent(EventId.OnWallPlacementStarted, null);
        Service.EventManager.SendEvent(EventId.DisplayMessage, Constants.SNOWBALL_SPAWN_TOOLTIP_TEXT);
        isPlacingWall = true;
        DisplayBuildableOptionAtIndex(currentWallOptionIndex);
    }

    private bool OnWallBuildingDisabled(object cookie)
    {
        if (isPlacingWall)
        {
            // Positioning wall before committing to placement.
            Service.EventManager.SendEvent(EventId.HideMessage, null);
            isPlacingWall = false;
            Destroy(ghostWall.gameObject);
            ghostWall = null;
        }
        return false;
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
        DefrostRangeFX.SetActive(false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Service.EventManager.RemoveListener(EventId.OnWallBuildingDisabled, OnWallBuildingDisabled);
        TeamName.OnValueChanged -= OnTeamNameChanged;
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        CurrentPlayerClass.OnValueChanged -= OnPlayerClassChanged;
        SnowCount.OnValueChanged -= OnSnowResourceChanged;
    }
}
