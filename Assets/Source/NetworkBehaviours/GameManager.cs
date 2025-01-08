using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

struct SpawnInfo
{
    public string TeamName;
    public Transform SpawnPoint;
}

public class GameManager : NetworkBehaviour
{
    public const string PLAYER_RESOURCE = "PlayerPrefab";
    private const string WALL_RESOURCE = "WallSegment";
    private const string SNOW_PILE_RESOURCE = "SnowPile";
    private const float SNOWBALL_THROW_SPEED = 1800f;
    private const float MIN_THROW_ANGLE = 5f;
    private const float MAX_THROW_ANGLE = 25f;
    private const float BLIZZARD_TIMEOUT = 90f;

    public float SnowballThrowSpeed = SNOWBALL_THROW_SPEED;
    public float MinThrowAngle = MIN_THROW_ANGLE;
    public float MaxThrowAngle = MAX_THROW_ANGLE;
    public GameState CurrentGameState { get; private set; }
    public PlayerEntity LocalPlayer 
    {
        get
        {
            if (localPlayer == null)
            {
                ulong localPlayerId = NetworkManager.Singleton.LocalClientId;
                Transform localPlayerTrans = playerTransforms[localPlayerId];
                localPlayer = localPlayerTrans.GetComponent<PlayerEntity>();
            }
            return localPlayer;
        }
    }
    private PlayerEntity localPlayer;
    private GameStartData startData;
    private GameObject levelPrefab;
    private Dictionary<string, List<Transform>> spawnPoints = new Dictionary<string, List<Transform>>();
    private Dictionary<string, List<ulong>> teamRosters = new Dictionary<string, List<ulong>>();
    private Dictionary<ulong, Transform> playerTransforms = new Dictionary<ulong, Transform>();
    private Dictionary<string, Transform> teamQueens = new Dictionary<string, Transform>();
    private Dictionary<GameObject, GameObject> walls = new Dictionary<GameObject, GameObject>();
    private PickupSystem pickupSystem;
    private List<BoxCollider> spawnVolumes;
    private float blizzardCountdown = BLIZZARD_TIMEOUT;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameManager: OnNetworkSpawn");
        CurrentGameState = GameState.PreGameLobby;
        if (IsServer)
        {
            Debug.Log("Server");
            pickupSystem = new PickupSystem(playerTransforms, OnSnowPickedUp);
            levelPrefab = Instantiate(Resources.Load<GameObject>(startData.LevelName));

            // Populate team spawn points
            GameObject spawnPointContainer = UnityUtils.FindGameObject(levelPrefab, "SpawnPoints");
            List<GameObject> teamSpawnPoints = UnityUtils.GetTopLevelChildren(spawnPointContainer);
            for (int i = 0, count = teamSpawnPoints.Count; i < count; ++i)
            {
                string teamName = teamSpawnPoints[i].name;
                List<Transform> spawnPointsTransforms = UnityUtils.GetTopLevelChildTransforms(teamSpawnPoints[i]);
                spawnPoints.Add(teamName, spawnPointsTransforms);
                teamRosters.Add(teamName, new List<ulong>());
            }

            Service.EventManager.AddListener(EventId.StartGameplayPressed, OnStartGameplayPressed);
            Service.UpdateManager.AddObserver(OnUpdate);
        }
        else
        {
            startData = GameObject.Find("Engine").GetComponent<Engine>().StartData;
            StartClient(startData);
        }

        Service.EventManager.SendEvent(EventId.GameManagerInitialized, IsHost);
        GetGameMetadataServerRpc(NetworkManager.LocalClientId);
    }

    // Triggered when the host presses the "Start Game" button.
    private bool OnStartGameplayPressed(object cookie)
    {
        Service.EventManager.RemoveListener(EventId.StartGameplayPressed, OnStartGameplayPressed);

        // Populate snowball spawn volumes
        GameObject spawnVolumesContaier = UnityUtils.FindGameObject(levelPrefab, "SnowballSpawnVolumes");
        spawnVolumes = UnityUtils.FindAllComponentsInChildren<BoxCollider>(spawnVolumesContaier);
        SpawnSnowballs(5);
        BroadcastGameStartRpc();
        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastGameStartRpc()
    {
        CurrentGameState = GameState.Gameplay;
        Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        Service.EventManager.AddListener(EventId.OnGamePause, OnGamePaused);
        Service.EventManager.AddListener(EventId.OnGameResume, OnGameResumed);
        Service.EventManager.AddListener(EventId.OnGameQuit, OnGameQuit);
    }

    // Entry point from Menu scene - triggers the network connection to be made.
    public void StartClient(GameStartData startData)
    {
        this.startData = startData;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        if (Constants.IS_OFFLINE_DEBUG)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void StartHost(GameStartData startData)
    {
        this.startData = startData;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        if (Constants.IS_OFFLINE_DEBUG)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client successfully disconnected from the server.");
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            Service.EventManager.RemoveListener(EventId.OnGamePause, OnGamePaused);
            Service.EventManager.RemoveListener(EventId.OnGameResume, OnGameResumed);
            Service.EventManager.RemoveListener(EventId.OnGameQuit, OnGameQuit);
            GameObject engineObj = GameObject.Find("Engine");
            if (engineObj != null)
            {
                Engine engine = engineObj.GetComponent<Engine>();
                engine.EndGame();
            }
        }
    }

    public bool OnGameQuit(object cookie)
    {
        Debug.Log($"Quit game for {NetworkManager.Singleton.LocalClientId}!");
        if (IsServer)
        {
            Service.UpdateManager.RemoveObserver(OnUpdate);
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            RequestQuitServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        return false;
    }

    private bool OnGamePaused(object cookie)
    {
        CurrentGameState = GameState.GameplayPaused;
        Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        return false;
    }

    private bool OnGameResumed(object cookie)
    {
        CurrentGameState = GameState.Gameplay;
        Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        return false;
    }

    [Rpc(SendTo.Server)]
    private void RequestQuitServerRpc(ulong clientId)
    {
        // Disconnect the client
        Debug.Log($"Quit game for {clientId}!");
        NetworkManager.Singleton.DisconnectClient(clientId);
    }

    private void OnUpdate(float dt)
    {
        blizzardCountdown -= dt;
        if (blizzardCountdown <= 0f)
        {
            SpawnSnowballs(5);
            blizzardCountdown = BLIZZARD_TIMEOUT;
        }
    }

    private SpawnInfo SelectTeamAndSpawnPos()
    {
        string selectedTeam = string.Empty;
        int smallestTeamCount = int.MaxValue;
        foreach (KeyValuePair<string, List<ulong>> roster in teamRosters)
        {
            int teamCount = roster.Value.Count;
            if (roster.Value.Count < smallestTeamCount)
            {
                selectedTeam = roster.Key;
                smallestTeamCount = teamCount;
            }
        }

        List<Transform> availableSpawns = spawnPoints[selectedTeam];
        Transform spawnPoint = availableSpawns[UnityEngine.Random.Range(0, availableSpawns.Count)];

        SpawnInfo result = new SpawnInfo();
        result.TeamName = selectedTeam;
        result.SpawnPoint = spawnPoint;
        return result;
    }

    // Sets up a new player and returns relevant game state information to the caller
    [Rpc(SendTo.Server)]
    private void GetGameMetadataServerRpc(ulong clientId)
    {
        Debug.Log("Requesting game data from server");
        SpawnInfo spawnInfo = SelectTeamAndSpawnPos();
        startData.PlayerTeamName = spawnInfo.TeamName;
        startData.PlayerStartPos = spawnInfo.SpawnPoint.position;
        startData.PlayerStartEuler = spawnInfo.SpawnPoint.eulerAngles;
        startData.PlayerId = clientId;
        startData.CurrentGameState = CurrentGameState == GameState.PreGameLobby ? GameState.PreGameLobby : GameState.Gameplay;

        SpawnPlayer(clientId, spawnInfo);
        AssignPlayerClass(startData.PlayerTeamName, clientId);

        Transform queenTransform = GetQueenForTeam(startData.PlayerTeamName);
        PlayerEntity player = queenTransform.GetComponent<PlayerEntity>();
        startData.TeamQueenPlayerId = player.OwnerClientId;

        ReceiveGameMetadataClientRpc(startData, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
    
    // Called on server only
    private void AssignPlayerClass(string teamName, ulong clientId)
    {
        bool isQueenAssigned = false;
        List<ulong> teamPlayerIds = teamRosters[teamName];
        for (int i = 0, count = teamPlayerIds.Count; i < count; ++i)
        {
            ulong playerId = teamPlayerIds[i];
            Transform playerTransform = playerTransforms[playerId];
            PlayerEntity player = playerTransform.GetComponent<PlayerEntity>();
            Debug.Log($"Player {playerId} class: {player.CurrentPlayerClass.Value}");
            if (player.CurrentPlayerClass.Value == PlayerClass.Queen)
            {
                isQueenAssigned = true;
                break;
            }
        }

        startData.PlayerClass = PlayerClass.Soldier;
        if (!isQueenAssigned)
        {
            startData.PlayerClass = PlayerClass.Queen;
            if (!teamQueens.ContainsKey(teamName))
            {
                teamQueens.Add(teamName, playerTransforms[clientId]);
            }
            else
            {
                teamQueens[teamName] = playerTransforms[clientId];
            }
            Debug.Log($"Promoting player {startData.PlayerName} to Queen role!");
        }
    }

    // SERVER CALLED ONLY
    private void SpawnPlayer(ulong clientId, SpawnInfo spawnInfo)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);
        teamRosters[spawnInfo.TeamName].Add(clientId);
    }

    // Server message sent back to a specific player when they join the game
    [Rpc(SendTo.SpecifiedInParams)]
    private void ReceiveGameMetadataClientRpc(GameStartData serverStartData, RpcParams clientRpcParams = default)
    {
        // Debug.Log("Received start data, level: " + serverStartData.LevelName);
        startData.LevelName = serverStartData.LevelName;
        startData.PlayerStartPos = serverStartData.PlayerStartPos;
        startData.PlayerStartEuler = serverStartData.PlayerStartEuler;
        startData.PlayerTeamName = serverStartData.PlayerTeamName;
        startData.PlayerClass = serverStartData.PlayerClass;
        startData.TeamQueenPlayerId = serverStartData.TeamQueenPlayerId;
        
        startData.CurrentGameState = serverStartData.CurrentGameState;
        CurrentGameState = startData.CurrentGameState;
        if (CurrentGameState != GameState.PreGameLobby)
        {
            Service.EventManager.AddListener(EventId.OnGamePause, OnGamePaused);
            Service.EventManager.AddListener(EventId.OnGameResume, OnGameResumed);
            Service.EventManager.AddListener(EventId.OnGameQuit, OnGameQuit);
        }

        LoadLevel();
    }

    // CLIENT CALLED ONLY
    private void LoadLevel()
    {
        Debug.Log("Load Level");
        if (!IsServer)
        {
            levelPrefab = Instantiate(Resources.Load<GameObject>(startData.LevelName));
            GameObject spawnPointContainer = UnityUtils.FindGameObject(levelPrefab, "SpawnPoints");
            List<GameObject> teamSpawnPoints = UnityUtils.GetTopLevelChildren(spawnPointContainer);
            for (int i = 0, count = teamSpawnPoints.Count; i < count; ++i)
            {
                string teamName = teamSpawnPoints[i].name;
                List<Transform> spawnPointsTransforms = UnityUtils.GetTopLevelChildTransforms(teamSpawnPoints[i]);
                spawnPoints.Add(teamName, spawnPointsTransforms);
                
                if (!teamRosters.ContainsKey(teamName))
                    teamRosters.Add(teamName, new List<ulong>());

                if (!teamQueens.ContainsKey(teamName))
                    teamQueens.Add(teamName, null);
            }
        }

        teamQueens[startData.PlayerTeamName] = playerTransforms[startData.TeamQueenPlayerId];
        Service.EventManager.SendEvent(EventId.LevelLoadCompleted, startData);

        if (CurrentGameState != GameState.PreGameLobby)
        {
            Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        }
    }

    [Rpc(SendTo.Server)]
    public void FireProjectileServerRpc(Vector3 position, Vector3 euler, Vector3 fwd, float verticalVel, ulong ownerId)
    {
        FireProjectileClientRpc(position, euler, fwd, verticalVel, ownerId);
    }

    [Rpc(SendTo.Everyone)]
    public void FireProjectileClientRpc(Vector3 position, Vector3 euler, Vector3 fwd, float verticalVel, ulong ownerId)
    {
        Transform owner = playerTransforms[ownerId];
        PlayerEntity player = owner.GetComponent<PlayerEntity>();
        if (player.SnowCount.Value <= 0)
        {
            Debug.Log("Not enough ammo!");
            return;
        }
        player.SetPlayerSnowCountServerRpc(player.SnowCount.Value - 1);

        Debug.Log("Locally firing projectile!");
        GameObject projectileObj = Instantiate(Resources.Load<GameObject>("LocalSnowball"));
        projectileObj.transform.position = position;
        projectileObj.transform.eulerAngles = euler;

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        rb.position = position;
        rb.rotation = Quaternion.identity;
        LocalProjectlie projComp = projectileObj.GetComponent<LocalProjectlie>();
        projComp.SetOwner(owner, IsServer);

        float throwAngle = Mathf.Lerp(MinThrowAngle, MaxThrowAngle, verticalVel) * Mathf.Deg2Rad;
        float verticalSpeed = SnowballThrowSpeed * Mathf.Sin(throwAngle);
        float horizontalSpeed = SnowballThrowSpeed * Mathf.Cos(throwAngle);
        rb.AddForce(new Vector3(fwd.x * horizontalSpeed, verticalSpeed, fwd.z * horizontalSpeed));
    }

    [Rpc(SendTo.Everyone)]
    public void TransmitProjectileHitClientRpc(ulong hitPlayerId)
    {
        PlayerEntity hitPlayer = playerTransforms[hitPlayerId].GetComponent<PlayerEntity>();
        hitPlayer.OnPlayerFrozen();

        if (IsServer)
        {
            Service.EventManager.SendEvent(EventId.PlayerHit, hitPlayer);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void TransmitGameOverRpc(string winningTeam)
    {
        CurrentGameState = GameState.PostGame;
        Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        Service.EventManager.SendEvent(EventId.OnGameOver, winningTeam);
    }

    // SERVER CALLED ONLY
    [Rpc(SendTo.Server)]
    public void ProjectileHitFloorServerRpc(Vector3 position)
    {
        GameObject instantiatedPile = Instantiate(Resources.Load<GameObject>(SNOW_PILE_RESOURCE));
        NetworkObject netObj = instantiatedPile.GetComponent<NetworkObject>();
        instantiatedPile.transform.position = position;
        instantiatedPile.transform.eulerAngles = new Vector3(-90f, Random.Range(0f, 360f), 0f);
        netObj.Spawn(true);
        pickupSystem.RegisterPickup(netObj.transform);
    }

    // This gets called on each client for each player entity in the game.
    // CLIENT CALLED ONLY
    public void RegisterPlayer(PlayerEntity player)
    {
        Debug.Log("Registering player " + player.OwnerClientId);
        playerTransforms.Add(player.OwnerClientId, player.transform);
        string playerTeamName = player.TeamName.Value.ToString();
        if (!teamRosters.ContainsKey(playerTeamName))
        {
            teamRosters.Add(playerTeamName, new List<ulong>());
        }
        teamRosters[playerTeamName].Add(player.OwnerClientId);
        
        if (!IsHost && player.CurrentPlayerClass.Value == PlayerClass.Queen)
        {
            Debug.Log($"Setting {playerTeamName} queen to {player.name}");
            teamQueens[playerTeamName] = player.transform;
        }
    }

    public void DeregisterPlayer(PlayerEntity player)
    {
        ulong playerId = player.OwnerClientId;
        Debug.Log("Deregistering player " + playerId);
        playerTransforms.Remove(playerId);
        string teamName = player.TeamName.Value.ToString();
        teamRosters[teamName].Remove(playerId);
        
        if (IsServer && !player.IsOwner && player.CurrentPlayerClass.Value == PlayerClass.Queen && !player.IsFrozen)
        {
            // ReassignQueenRpc(teamName);
            List<ulong> teamIds = teamRosters[teamName];
            if (teamIds.Count > 0)
            {
                // AssignPlayerClass(teamName, teamIds[0]);
                Transform playerTransform = playerTransforms[teamRosters[teamName][0]];
                PlayerEntity entity = playerTransform.GetComponent<PlayerEntity>();
                entity.AssignPlayerClassServerRpc(PlayerClass.Queen);
                teamQueens[teamName] = playerTransform;
            }
        }
    }

    public void BroadcastRosterUpdate()
    {
        Dictionary<string, List<string>> roster = new Dictionary<string, List<string>>();

        foreach (KeyValuePair<ulong, Transform> pair in playerTransforms)
        {
            Transform playerTransform = pair.Value;
            PlayerEntity player = playerTransform.GetComponent<PlayerEntity>();
            string playerTeam = player.TeamName.Value.ToString();
            
            if (!roster.ContainsKey(playerTeam))
                roster.Add(playerTeam, new List<string>());

            roster[playerTeam].Add(player.name);
        }

        Service.EventManager.SendEvent(EventId.PlayerRosterUpdated, roster);
    }

    int numPlayersSetUp;
    public void OnPlayerFullySetUp()
    {
        ++numPlayersSetUp;
        if (numPlayersSetUp == NetworkManager.ConnectedClientsIds.Count)
        {
            Debug.Log("All players fully set up!");
        }
    }

    public Transform GetQueenForTeam(string teamName)
    {
        return teamQueens[teamName];
    }

    public Dictionary<string, List<PlayerEntity>> GetTeamRosters()
    {
        Dictionary<string, List<PlayerEntity>> returnRoster = new Dictionary<string, List<PlayerEntity>>();
        foreach (KeyValuePair<string, List<ulong>> roster in teamRosters)
        {
            List<PlayerEntity> teamEntities = new List<PlayerEntity>();
            int teamCount = roster.Value.Count;
            for (int i = 0; i < teamCount; ++i)
            {
                PlayerEntity entity = playerTransforms[roster.Value[i]].GetComponent<PlayerEntity>();
                teamEntities.Add(entity);
            }
            returnRoster.Add(roster.Key, teamEntities);
        }
        return returnRoster;
    }

    [Rpc(SendTo.Server)]
    public void SpawnWallServerRpc(string resourceName, Vector3 position, Vector3 euler, ulong ownerId)
    {
        Transform playerTransform = playerTransforms[ownerId];
        PlayerEntity player = playerTransform.GetComponent<PlayerEntity>();
        if (player.SnowCount.Value < Constants.WALL_COST)
        {
            Debug.Log($"Player {ownerId}: Not enough Snowballs to make a wall!");
            return;
        }
        player.SetPlayerSnowCountServerRpc(player.SnowCount.Value - Constants.WALL_COST);

        GameObject instantiatedWall = Instantiate(Resources.Load<GameObject>(resourceName));
        NetworkObject netObj = instantiatedWall.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        instantiatedWall.transform.position = position;
        instantiatedWall.transform.eulerAngles = euler;
        Rigidbody rigidBody = instantiatedWall.GetComponent<Rigidbody>();
        rigidBody.position = position;
        rigidBody.rotation = Quaternion.Euler(euler);

        GameObject toppleCollider = UnityUtils.FindGameObject(instantiatedWall, "ToppleDetection");
        CollisionEventDispatcher collisionEvents = toppleCollider.GetComponent<CollisionEventDispatcher>();
        collisionEvents.AddListenerCollisionStart(OnWallToppled);
        walls.Add(toppleCollider, instantiatedWall);
    }

    private void OnWallToppled(GameObject toppleDetector)
    {
        GameObject wallObject = walls[toppleDetector];
        NetworkObject netObj = wallObject.GetComponent<NetworkObject>();
        netObj.Despawn();
        walls.Remove(toppleDetector);
    }

    // SERVER CALLED ONLY
    private void OnSnowPickedUp(Transform pickup, PlayerEntity player)
    {
        player.SetPlayerSnowCountServerRpc(player.SnowCount.Value + 1);
        pickupSystem.UnregisterPickup(pickup);
        Destroy(pickup.gameObject);
    }

    private void SpawnSnowballs(int numToSpawnPerSide)
    {
        List<Vector3> spawnPositions = new List<Vector3>();
        for (int i = 0, count = spawnVolumes.Count; i < count; ++i)
        {
            BoxCollider volume = spawnVolumes[i];
            for (int j = 0; j < numToSpawnPerSide; ++j)
            {
                float xVal = Random.Range(volume.bounds.min.x, volume.bounds.max.x);
                float yVal = Random.Range(volume.bounds.min.y, volume.bounds.max.y);
                float zVal = Random.Range(volume.bounds.min.z, volume.bounds.max.z);
                spawnPositions.Add(new Vector3(xVal, yVal, zVal));
            }
        }
        SpawnSnowballsClientRpc(spawnPositions.ToArray());
    }

    [Rpc(SendTo.Everyone)]
    public void SpawnSnowballsClientRpc(Vector3[] spawnPositions)
    {
        for (int i = 0, count = spawnPositions.Length; i < count; ++i)
        {
            Vector3 position = spawnPositions[i];
            Debug.Log("Locally firing projectile!");
            GameObject projectileObj = Instantiate(Resources.Load<GameObject>("LocalSnowball"));
            projectileObj.transform.position = position;

            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            rb.position = position;
            rb.rotation = Quaternion.identity;
            LocalProjectlie projComp = projectileObj.GetComponent<LocalProjectlie>();
            projComp.SetOwner(null, IsServer);
        }
    }
}
