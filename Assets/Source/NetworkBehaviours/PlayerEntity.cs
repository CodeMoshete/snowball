using System.Collections.Generic;
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
    private const string THROWABLES_RESOURCE = "ThrowableObjects/Throwables";
    public float Speed = 5;
    public float RotationSpeed = 40f;
    public Transform ProjectileOriginReference;
    public GameObject DefrostRangeFX;
    public NetworkVariable<FixedString64Bytes> TeamName = new NetworkVariable<FixedString64Bytes>(Constants.TEAM_UNASSIGNED);
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(Constants.PLAYER_NAME_DEFAULT);
    public NetworkVariable<PlayerClass> CurrentPlayerClass = new NetworkVariable<PlayerClass>(global::PlayerClass.Soldier);

    public bool IsFrozen { get; private set; }
    public bool IsControlDisabled { 
        get
        {
            return IsFrozen || gameManager.CurrentGameState != GameState.Gameplay;
        }}
    private Transform iceCube;
    private Renderer iceCubeRenderer;
    private float frozenTimer;
    private List<ThawSource> thawSourcesInRange = new List<ThawSource>();
    public float Health { get; private set; } = Constants.MAX_HEALTH;
    
    private bool isPlacingWall;
    private Transform ghostWall;
    private Buildables wallOptions;
    private int currentWallOptionIndex;

    private List<SnowballInventoryItem> snowballInventory;
    private int currentSnowballTypeIndex;

    private PlayerEntityControls controls;

    private GameManager gameManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        name = PlayerName.Value.ToString();
        TeamName.OnValueChanged += OnTeamNameChanged;
        PlayerName.OnValueChanged += OnPlayerNameChanged;
        CurrentPlayerClass.OnValueChanged += OnPlayerClassChanged;
        wallOptions = Resources.Load<Buildables>(BUILDABLES_RESOURCE);
        
        Constants.SnowballTypes = Resources.Load<Throwables>(THROWABLES_RESOURCE);
        snowballInventory = new List<SnowballInventoryItem>();
        // Pre-fill each ammo type with 0 quantity.
        for (int i = 0; i < Constants.SnowballTypes.ThrowableObjects.Count; i++)
        {
            snowballInventory.Add(new SnowballInventoryItem
            {
                ThrowableObject = Constants.SnowballTypes.ThrowableObjects[i],
                Quantity = 0
            });
        }

        gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
        gameManager.RegisterPlayerTransform(this);
        if (IsOwner)
        {
            Debug.Log("Player OnNetworkSpawn - Setting up new player!");
            Service.EventManager.AddListener(EventId.LevelLoadCompleted, OnLevelLoadComplete);
            Service.EventManager.AddListener(EventId.OnWallBuildingDisabled, OnWallBuildingDisabled);
            Service.DataStreamManager.UpdateFloatDataStream(FloatDataStream.PlayerHealth, Health);
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

    [Rpc(SendTo.Everyone)]
    public void SetPlayerSnowCountClientRpc(SnowballType type, int newAmount)
    {
        SnowballInventoryItem inventoryItem = GetInventoryForType(type);
        inventoryItem.Quantity = newAmount;

        Debug.Log($"Snow count {type} for player {OwnerClientId} changed to {newAmount} : {IsOwner}");
        if (IsOwner)
        {
            Service.EventManager.SendEvent(EventId.AmmoUpdated, inventoryItem);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetPlayerColorRpc(Color newColor)
    {
        GetComponent<Renderer>().material.color = newColor;
    }

    public SnowballInventoryItem GetInventoryForType(SnowballType type)
    {
        return snowballInventory.Find(item => item.ThrowableObject.Type == type);
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
            TestQueenInRange(dt);
            TestThawSourceInRange(dt);
        }
        else if (Health < Constants.MAX_HEALTH)
        {
            Health += Constants.HEALTH_RECHARGE_RATE_PER_SEC * dt;
            Health = Mathf.Min(Health, Constants.MAX_HEALTH);

            if (IsOwner)
            {
                Service.DataStreamManager.UpdateFloatDataStream(FloatDataStream.PlayerHealth, Health);
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

    private void TestQueenInRange(float dt)
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
                UpdateUnfreezeVisuals();
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

    private void TestThawSourceInRange(float dt)
    {
        for (int i = 0, count = thawSourcesInRange.Count; i < count; i++)
        {
            frozenTimer -= dt;
            UpdateUnfreezeVisuals();
            if (IsServer && frozenTimer <= 0f)
            {
                OnPlayerUnfrozenClientRpc();
            }
            break;
        }
    }

    private void UpdateUnfreezeVisuals()
    {
        float pct = 1f - (frozenTimer / UNFREEZE_SECONDS);
        Color unfreezeColor = Color.Lerp(FROZEN_COLOR, UNFROZEN_COLOR, pct);
        iceCubeRenderer.material.SetColor("_BaseColor", unfreezeColor);
    }

    public void SetThawSourceInRange(ThawSource source)
    {
        Debug.Log($"Player {name} is in range of thaw source {source.name}");
        thawSourcesInRange.Add(source);
    }

    public void RemoveThawSourceInRange(ThawSource source)
    {
        Debug.Log($"Player {name} is NOT in range of thaw source {source.name}");
        thawSourcesInRange.Remove(source);
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

    public void OnCycleSnowballAmmoPressed()
    {
        if (currentSnowballTypeIndex >= snowballInventory.Count - 1)
        {
            currentSnowballTypeIndex = 0;
        }
        else
        {
            currentSnowballTypeIndex++;
        }
        Service.EventManager.SendEvent(EventId.AmmoTypeCycled, snowballInventory[currentSnowballTypeIndex]);
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
                OwnerClientId,
                snowballInventory[currentSnowballTypeIndex].ThrowableObject.Type
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
        if (GetInventoryForType(SnowballType.Basic).Quantity < Constants.WallCost)
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

    [Rpc(SendTo.Everyone)]
    public void SetPlayerPositionFromScriptRpc(Vector3 newPosition)
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        transform.position = newPosition;
        Debug.Log("[PlayerEntity] Set player position to " + newPosition.ToString());
    }

    public void FreezePlayerFromScript()
    {
        if (!IsFrozen)
        {
            gameManager.TransmitProjectileHitClientRpc(-1, OwnerClientId, Constants.MAX_HEALTH);
        }
    }

    public bool OnPlayerHit(float damage)
    {
        if (IsFrozen)
            return false;

        Health -= damage;
        if (Health <= 0f)
        {
            Health = 0f;
            OnPlayerFrozen();
        }

        if (IsOwner)
        {
            Service.DataStreamManager.UpdateFloatDataStream(FloatDataStream.PlayerHealth, Health);
        }

        return Health <= 0;
    }

    public void DamagePlayerFromScript(float damage, long throwingPlayerId)
    {
        if (!IsFrozen)
        {
            gameManager.TransmitProjectileHitClientRpc(throwingPlayerId, OwnerClientId, damage);
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
        Health = Constants.MAX_HEALTH;
        if (IsOwner)
        {
            Service.DataStreamManager.UpdateFloatDataStream(FloatDataStream.PlayerHealth, Health);
        }
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
    }
}
