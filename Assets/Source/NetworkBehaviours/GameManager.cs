using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Utils;

struct SpawnInfo
{
    public string TeamName;
    public Transform SpawnPoint;
}

public class GameManager : NetworkBehaviour
{
    // public const string PLAYER_RESOURCE = "PlayerPrefab";
    public const string PLAYER_RESOURCE = "PlayerPrefabModel";
    private const string SNOW_PILE_RESOURCE = "SnowPile";

    public float SnowballThrowSpeed = Constants.SNOWBALL_THROW_SPEED;
    public float MinThrowAngle = Constants.MIN_THROW_ANGLE;
    public float MaxThrowAngle = Constants.MAX_THROW_ANGLE;
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
    private Dictionary<string, List<PlayerEntity>> teams = new Dictionary<string,List<PlayerEntity>>();
    public Dictionary<string, List<PlayerEntity>> Teams
    {
        get
        {
            return teams;
        }
    }
    private Dictionary<string, Color> teamColors = new Dictionary<string, Color>();
    private Dictionary<string, Transform> teamQueens = new Dictionary<string, Transform>();
    private Dictionary<GameObject, GameObject> walls = new Dictionary<GameObject, GameObject>();
    private PickupSystem pickupSystem;
    private List<BoxCollider> spawnVolumes;
    private AudioSource soundEffectPlayer;
    // private float blizzardCountdown = Constants.BLIZZARD_TIMEOUT;
    private bool didInitQuit;

    // 1. Entry point for newly loaded player.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameManager: OnNetworkSpawn");
        soundEffectPlayer = GetComponent<AudioSource>();
        CurrentGameState = GameState.PreGameLobby;
        if (IsServer)
        {
            // Set up level data.
            OnServerNetworkSpawn();
        }
        else
        {
            startData = GameObject.Find("Engine").GetComponent<Engine>().StartData;
            StartClient(startData);
            GetGameMetadataServerRpc(NetworkManager.LocalClientId, startData.PlayerName);
        }
    }

    // 2a. Server only - sets up only level related data.
    // Does not handle any player data.
    private void OnServerNetworkSpawn()
    {
        Debug.Log("Server");
        pickupSystem = new PickupSystem(playerTransforms, OnSnowPickedUp);
        Service.LevelLoader.LoadLevel(startData.LevelName, OnServerLevelPrefabLoaded, OnLevelLoadFail);
    }

    private void OnServerLevelPrefabLoaded(GameObject prefab)
    {
        levelPrefab = Instantiate(prefab);
        // levelPrefab = prefab;
        Service.NetworkActions.RegisterNetworkActionsForLevel(levelPrefab);

        // Populate team spawn points
        GameObject spawnPointContainer = UnityUtils.FindGameObject(levelPrefab, "SpawnPoints");
        List<GameObject> teamSpawnPoints = UnityUtils.GetTopLevelChildren(spawnPointContainer);
        for (int i = 0, count = teamSpawnPoints.Count; i < count; ++i)
        {
            string teamName = teamSpawnPoints[i].name;
            
            TeamColorProvider teamColorProvider = teamSpawnPoints[i].GetComponent<TeamColorProvider>();
            if (teamColorProvider != null)
            {
                Color teamColor = teamColorProvider.TeamColor;
                teamColors.Add(teamName, teamColor);
            }

            List<Transform> spawnPointsTransforms = UnityUtils.GetTopLevelChildTransforms(teamSpawnPoints[i]);
            spawnPoints.Add(teamName, spawnPointsTransforms);
            teamRosters.Add(teamName, new List<ulong>());
        }

        int teamNum = 0;
        foreach (KeyValuePair<string, List<Transform>> teamSpawns in spawnPoints)
        {
            string teamName = teamSpawns.Key;
            if (!teamColors.ContainsKey(teamName))
            {
                Color teamColor = Constants.TEAM_COLORS[teamNum];
                teamColors.Add(teamName, teamColor);
            }
            ++teamNum;
        }

        Service.EventManager.AddListener(EventId.NetworkActionTriggered, OnNetworkAction);
        Service.EventManager.AddListener(EventId.OnDeSpawnNetworkObject, OnDeSpawnNetworkObject);
        Service.EventManager.AddListener(EventId.StartGameplayPressed, OnStartGameplayPressed);
        // Service.EventManager.AddListener(EventId.OnPlaySoundEffect, OnPlaySoundEffect);
        Service.EventManager.AddListener(EventId.OnSpawnLocalGameObject, OnSpawnLocalGameObject);
        GameInitializationData initData = GetGameInitData(levelPrefab);
        Service.EventManager.SendEvent(EventId.GameManagerInitialized, initData);
        GetGameMetadataServerRpc(NetworkManager.LocalClientId, startData.PlayerName);
    }

    // 2b. Entry point from Menu scene - triggers the network connection to be made.
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

    // This gets called on each client every time a new player entity is Spawned.
    // CLIENT CALLED ONLY
    public void RegisterPlayerTransform(PlayerEntity player)
    {
        Debug.Log("Registering player transform " + player.OwnerClientId);
        playerTransforms.Add(player.OwnerClientId, player.transform);
    }

    private SpawnInfo SelectTeamAndSpawnPos()
    {
        string selectedTeam = string.Empty;
        int smallestTeamCount = int.MaxValue;
        foreach (KeyValuePair<string, List<ulong>> roster in teamRosters)
        {
            if (roster.Key == Constants.TEAM_UNASSIGNED)
                continue;

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

    // 3. Sets up a new player and returns relevant game state information to the caller
    [Rpc(SendTo.Server)]
    private void GetGameMetadataServerRpc(ulong clientId, string playerName)
    {
        Debug.Log("Requesting game data from server");
        SpawnInfo spawnInfo = SelectTeamAndSpawnPos();
        startData.PlayerTeamName = spawnInfo.TeamName;
        startData.PlayerStartPos = spawnInfo.SpawnPoint.position;
        startData.PlayerStartEuler = spawnInfo.SpawnPoint.eulerAngles;
        startData.PlayerId = clientId;
        startData.PlayerName = playerName;
        
        startData.CurrentGameState = CurrentGameState ==
            GameState.PreGameLobby ? GameState.PreGameLobby : GameState.Gameplay;

        startData.StartActions = Service.NetworkActions.CurrentActionsToSync;
        startData.PlayerColor = teamColors[startData.PlayerTeamName];

        SpawnPlayer(clientId, playerName, spawnInfo);
        AssignPlayerClass(startData.PlayerTeamName, clientId);

        Transform queenTransform = GetQueenForTeam(startData.PlayerTeamName);
        PlayerEntity queenPlayer = queenTransform.GetComponent<PlayerEntity>();

        startData.TeamQueenPlayerId = queenPlayer.OwnerClientId;

        ReceiveGameMetadataClientRpc(startData, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        // player.SetPlayerSnowCountClientRpc(SnowballType.Basic, Constants.DEFAULT_START_AMMO);
    }

    // SERVER CALLED ONLY
    private PlayerEntity SpawnPlayer(ulong clientId, string clientName, SpawnInfo spawnInfo)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        PlayerEntity entity = instantiatedPlayer.GetComponent<PlayerEntity>();
        entity.PlayerName.Value = clientName;
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);
        teamRosters[spawnInfo.TeamName].Add(clientId);
        entity.SetPlayerSnowCountClientRpc(SnowballType.Basic, Constants.DEFAULT_START_AMMO);
        Debug.Log($"Added player {clientId} to team {spawnInfo.TeamName}");
        return entity;
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

    // Server message sent back to a specific player when they join the game
    [Rpc(SendTo.SpecifiedInParams)]
    private void ReceiveGameMetadataClientRpc(GameStartData serverStartData, RpcParams clientRpcParams = default)
    {
        // Debug.Log("Received start data, level: " + serverStartData.LevelName);
        startData.LevelName = serverStartData.LevelName;
        startData.PlayerStartPos = serverStartData.PlayerStartPos;
        startData.PlayerStartEuler = serverStartData.PlayerStartEuler;
        startData.PlayerTeamName = serverStartData.PlayerTeamName;
        startData.PlayerName = serverStartData.PlayerName;
        startData.PlayerClass = serverStartData.PlayerClass;
        startData.TeamQueenPlayerId = serverStartData.TeamQueenPlayerId;
        startData.StartActions = serverStartData.StartActions;
        startData.PlayerColor = serverStartData.PlayerColor;

        startData.CurrentGameState = serverStartData.CurrentGameState;
        CurrentGameState = startData.CurrentGameState;
        if (CurrentGameState != GameState.PreGameLobby)
        {
            Service.EventManager.AddListener(EventId.OnGamePause, OnGamePaused);
            Service.EventManager.AddListener(EventId.OnGameResume, OnGameResumed);
            Service.EventManager.AddListener(EventId.OnGameQuit, OnGameQuit);
        }

        // LoadLevel();

        if (!IsServer)
        {
            // Client must load the level after receiving start data from the server.
            Service.LevelLoader.LoadLevel(startData.LevelName, OnClientLevelPrefabLoaded, OnLevelLoadFail);
        }
        else
        {
            // The server has already loaded the level by this point, and is ready to start the game.
            teamQueens[startData.PlayerTeamName] = playerTransforms[startData.TeamQueenPlayerId];
            Service.EventManager.SendEvent(EventId.LevelLoadCompleted, startData);

            if (CurrentGameState != GameState.PreGameLobby)
            {
                Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
            }
        }
    }

    private void OnClientLevelPrefabLoaded(GameObject levelObject)
    {
        Debug.Log("Load Level");
        if (!IsServer)
        {
            levelPrefab = Instantiate(levelObject);

            Service.NetworkActions.RegisterNetworkActionsForLevel(levelPrefab);
            Service.EventManager.AddListener(EventId.NetworkActionTriggered, OnNetworkAction);
            Service.NetworkActions.SyncActionsForLateJoiningUser(startData.StartActions);

            List<GameObject> levelInfoContainer = UnityUtils.FindAllGameObjectContains<LevelInfo>(levelPrefab);
            GameInitializationData initData = GetGameInitData(levelPrefab);
            Service.EventManager.SendEvent(EventId.GameManagerInitialized, initData);

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

    private GameInitializationData GetGameInitData(GameObject levelPrefab)
    {
        List<GameObject> levelInfoContainer = UnityUtils.FindAllGameObjectContains<LevelInfo>(levelPrefab);
            LevelInfo levelInfo = null;
            if (levelInfoContainer.Count > 0)
            {
                levelInfo = levelInfoContainer[0].GetComponent<LevelInfo>();
            }

            GameInitializationData initData = new GameInitializationData();
            initData.IsHost = IsHost;
            initData.LevelInfo = levelInfo;
            return initData;
    }

    private void OnLevelLoadFail()
    {
        Debug.LogError("Level load failed.");
    }

    // Triggered when the host presses the "Start Game" button.
    private bool OnStartGameplayPressed(object cookie)
    {
        Service.EventManager.RemoveListener(EventId.StartGameplayPressed, OnStartGameplayPressed);

        // Populate snowball spawn volumes
        GameObject spawnVolumesContaier = UnityUtils.FindGameObject(levelPrefab, "SnowballSpawnVolumes");
        spawnVolumes = UnityUtils.FindAllComponentsInChildren<BoxCollider>(spawnVolumesContaier);
        // SpawnSnowballs(5);
        BroadcastGameStartRpc();
        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastGameStartRpc()
    {
        CurrentGameState = GameState.Gameplay;
        if (IsServer)
        {
            Service.EventManager.AddListener(EventId.OnSnowballsSpawnedFromScript, OnSnowballsSpanwedFromScript);
        }
        Service.EventManager.SendEvent(EventId.GameStateChanged, CurrentGameState);
        Service.EventManager.AddListener(EventId.OnGamePause, OnGamePaused);
        Service.EventManager.AddListener(EventId.OnGameResume, OnGameResumed);
        Service.EventManager.AddListener(EventId.OnGameQuit, OnGameQuit);
        Service.EventManager.AddListener(EventId.OnPlaySoundEffect, OnPlaySoundEffect);
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
            Service.EventManager.RemoveListener(EventId.NetworkActionTriggered, OnNetworkAction);
            Service.EventManager.RemoveListener(EventId.OnSnowballsSpawnedFromScript, OnSnowballsSpanwedFromScript);
            Service.EventManager.RemoveListener(EventId.OnPlaySoundEffect, OnPlaySoundEffect);
            Service.EventManager.RemoveListener(EventId.OnDeSpawnNetworkObject, OnDeSpawnNetworkObject);
            Service.EventManager.RemoveListener(EventId.OnSpawnLocalGameObject, OnSpawnLocalGameObject);
            Service.NetworkActions.ClearNetworkActionsForCurrentLevel();
            Constants.ResetDefaultValues();

            GameObject engineObj = GameObject.Find("Engine");
            if (engineObj != null)
            {
                Engine engine = engineObj.GetComponent<Engine>();
                // TERRIBLE HACK to work around Unity Multiplayer Widget's awful Leave Session technical limitations.
                if (!didInitQuit)
                {
                    Service.EventManager.SendEvent(EventId.OnGamePause, null);
                    Service.EventManager.SendEvent(EventId.GameStateChanged, GameState.GameplayPaused);
                    Button leaveButton = GameObject.Find("Leave Session").GetComponent<Button>();
                    engine.StartEndSequence(leaveButton.onClick);
                }
                else
                {
                    engine.EndGame();
                }
            }
        }
    }

    public bool OnGameQuit(object cookie)
    {
        didInitQuit = true;
        Debug.Log($"Quit game for {NetworkManager.Singleton.LocalClientId}!");
        if (IsServer)
        {
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

    [Rpc(SendTo.Server)]
    public void FireProjectileServerRpc(Vector3 position, Vector3 euler, Vector3 fwd, float verticalVel, ulong ownerId, SnowballType type = SnowballType.Basic)
    {
        Transform owner = playerTransforms[ownerId];
        PlayerEntity player = owner.GetComponent<PlayerEntity>();
        SnowballInventoryItem playerInventory = player.GetInventoryForType(type);
        if (playerInventory.Quantity <= 0)
        {
            Debug.Log("Not enough ammo!");
            return;
        }
        player.SetPlayerSnowCountClientRpc(type, playerInventory.Quantity - 1);

        FireProjectileClientRpc(position, euler, fwd, verticalVel, ownerId, type);
    }

    [Rpc(SendTo.Everyone)]
    public void FireProjectileClientRpc(Vector3 position, Vector3 euler, Vector3 fwd, float pitchPct, ulong ownerId, SnowballType type = SnowballType.Basic)
    {
        Transform owner = playerTransforms[ownerId];
        PlayerEntity player = owner.GetComponent<PlayerEntity>();
        player.ThrowAudioSource?.Play();

        Debug.Log("Locally firing projectile!");
        ThrowableObject thrownSnowball = Constants.SnowballTypes.ThrowableObjects.Find(item => item.Type == type);
        GameObject projectileObj = Instantiate(Resources.Load<GameObject>(thrownSnowball.PrefabName));
        // GameObject projectileObj = Instantiate(Resources.Load<GameObject>("LocalSnowballTeleport"));
        projectileObj.transform.position = position;
        projectileObj.transform.eulerAngles = euler;

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        rb.position = position;
        rb.rotation = Quaternion.identity;
        LocalProjectlie projComp = projectileObj.GetComponent<LocalProjectlie>();
        projComp.SetOwner(owner, IsServer);

        // Limits throw pitch to look values above 0.4.
        float loftPct = projComp.customPitchAngles ? pitchPct : Mathf.Max(pitchPct - 0.4f, 0f) / 0.6f;

        float minAgle = projComp.customPitchAngles ? projComp.MinPitch : MinThrowAngle;
        float maxAgle = projComp.customPitchAngles ? projComp.MaxPitch : MaxThrowAngle;

        float throwAngle = Mathf.Lerp(minAgle, maxAgle, loftPct) * Mathf.Deg2Rad;
        float verticalSpeed = SnowballThrowSpeed * Mathf.Sin(throwAngle);
        float horizontalSpeed = SnowballThrowSpeed * Mathf.Cos(throwAngle);
        Vector3 forceDirection = new Vector3(fwd.x * horizontalSpeed, verticalSpeed, fwd.z * horizontalSpeed);
        rb.AddForce(forceDirection);
        projectileObj.transform.LookAt(projectileObj.transform.position + forceDirection);
    }

    [Rpc(SendTo.Everyone)]
    public void TransmitProjectileHitClientRpc(long thrownPlayerId, ulong hitPlayerId, float damage)
    {
        PlayerEntity hitPlayer = playerTransforms[hitPlayerId].GetComponent<PlayerEntity>();
        PlayerEntity throwingPlayer = thrownPlayerId >= 0 ? playerTransforms[(ulong)thrownPlayerId].GetComponent<PlayerEntity>() : null;
        bool isFrozen = hitPlayer.OnPlayerHit(damage);

        PlayerHitData hitData = new PlayerHitData();
        hitData.ThrowingPlayer = throwingPlayer;
        hitData.HitPlayer = hitPlayer;
        hitData.DamageAmount = damage;

        if (isFrozen)
        {
            if (hitPlayerId == LocalPlayer.OwnerClientId)
            {
                hitData.Outcome = PlayerFrozenState.LocalPlayerFrozen;
            }
            else if (throwingPlayer != null && throwingPlayer.OwnerClientId == LocalPlayer.OwnerClientId)
            {
                if (throwingPlayer.TeamName.Value == hitPlayer.TeamName.Value)
                {
                    hitData.Outcome = PlayerFrozenState.LocalPlayerFrozeTeammate;
                    Service.EventManager.SendEvent(EventId.OnPlaySoundEffect, "Audio/TeamkillSound");
                }
                else
                {
                    hitData.Outcome = PlayerFrozenState.LocalPlayerFrozeEnemy;
                    Service.EventManager.SendEvent(EventId.OnPlaySoundEffect, "Audio/Boink");
                }
            }
            else if (hitPlayer.TeamName.Value == LocalPlayer.TeamName.Value)
            {
                if (hitPlayer.CurrentPlayerClass.Value == PlayerClass.Queen)
                {
                    hitData.Outcome = PlayerFrozenState.AllyQueenFrozen;
                }
                else
                {
                    hitData.Outcome = PlayerFrozenState.AllyFrozen;
                }
            }
            else
            {
                hitData.Outcome = PlayerFrozenState.EnemyFrozen;
            }

            Service.EventManager.SendEvent(EventId.PlayerFrozen, hitData);
        }
        else
        {
            hitData.Outcome = PlayerFrozenState.NotFrozen;
        }

        Service.EventManager.SendEvent(EventId.PlayerHit, hitData);
    }

    private bool OnSpawnLocalGameObject(object cookie)
    {
        SpawnResourceEventData spawnData = (SpawnResourceEventData)cookie;
        TransmitLocalGameObjectSpawnedRpc(spawnData.ResourcePath, spawnData.Position, spawnData.Rotation);
        return true;
    }

    [Rpc(SendTo.Everyone)]
    public void TransmitLocalGameObjectSpawnedRpc(string resourceName, Vector3 position, Vector3 euler)
    {
        GameObject instantiatedObj = Instantiate(Resources.Load<GameObject>(resourceName));
        instantiatedObj.transform.position = position;
        instantiatedObj.transform.eulerAngles = euler;
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
    public void ProjectileHitFloorServerRpc(Vector3 position, SnowballType type)
    {
        ThrowableObject snowball = Constants.SnowballTypes.ThrowableObjects.Find(item => item.Type == type);
        GameObject instantiatedPile = Instantiate(Resources.Load<GameObject>(snowball.PickupPrefabName));
        NetworkObject netObj = instantiatedPile.GetComponent<NetworkObject>();
        instantiatedPile.transform.position = position;
        instantiatedPile.transform.eulerAngles = new Vector3(-90f, Random.Range(0f, 360f), 0f);
        netObj.Spawn(true);
        pickupSystem.RegisterPickup(netObj.transform);
    }

    // SERVER CALLED ONLY
    [Rpc(SendTo.Server)]
    public void ProjectileHitObjectiveServerRpc(long thrownPlayerId, string objectiveName)
    {
        PlayerEntity throwingPlayer = thrownPlayerId >= 0 ? playerTransforms[(ulong)thrownPlayerId].GetComponent<PlayerEntity>() : null;
        ObjectiveHitData hitData = new ObjectiveHitData();
        hitData.ObjectiveName = objectiveName;
        hitData.ThrowingPlayer = throwingPlayer;
        Service.EventManager.SendEvent(EventId.ObjectiveHit, hitData);
    }

    public void DeregisterPlayer(PlayerEntity player)
    {
        ulong playerId = player.OwnerClientId;
        playerTransforms.Remove(playerId);
        teams[player.TeamName.Value.ToString()].Remove(player);
        string teamName = player.TeamName.Value.ToString();
        teamRosters[teamName].Remove(playerId);
        Debug.Log($"Removed player {player.OwnerClientId} from team {teamName}");
        Debug.Log($"Deregistered player {playerId} - team size {teamRosters[teamName].Count}");

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
        teams = new Dictionary<string, List<PlayerEntity>>();

        foreach (KeyValuePair<ulong, Transform> pair in playerTransforms)
        {
            Transform playerTransform = pair.Value;
            PlayerEntity player = playerTransform.GetComponent<PlayerEntity>();
            string playerTeam = player.TeamName.Value.ToString();

            if (!roster.ContainsKey(playerTeam))
                roster.Add(playerTeam, new List<string>());

            roster[playerTeam].Add(player.name);

            if (!teams.ContainsKey(playerTeam))
                teams.Add(playerTeam, new List<PlayerEntity>());

            teams[playerTeam].Add(player);
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
            if (roster.Key == Constants.TEAM_UNASSIGNED)
                continue;

            List<PlayerEntity> teamEntities = new List<PlayerEntity>();
            int teamSize = roster.Value.Count;
            for (int i = 0; i < teamSize; ++i)
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
        // if (player.SnowCount.Value < Constants.WallCost)
        // {
        //     Debug.Log($"Player {ownerId}: Not enough Snowballs to make a wall!");
        //     return;
        // }
        // player.SetPlayerSnowCountServerRpc(player.SnowCount.Value - Constants.WallCost);

        SnowballInventoryItem playerInventory = player.GetInventoryForType(SnowballType.Basic);
        if (playerInventory.Quantity < Constants.WallCost)
        {
            Debug.Log($"Player {ownerId}: Not enough Snowballs to make a wall!");
            return;
        }
        player.SetPlayerSnowCountClientRpc(SnowballType.Basic, playerInventory.Quantity - Constants.WallCost);

        GameObject instantiatedWall = Instantiate(Resources.Load<GameObject>(resourceName));
        instantiatedWall.transform.position = position;
        instantiatedWall.transform.eulerAngles = euler;
        NetworkObject netObj = instantiatedWall.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        Rigidbody rigidBody = instantiatedWall.GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            rigidBody.position = position;
            rigidBody.rotation = Quaternion.Euler(euler);
        }

        GameObject toppleCollider = UnityUtils.FindGameObject(instantiatedWall, "ToppleDetection");
        if (toppleCollider != null)
        {
            CollisionEventDispatcher collisionEvents = toppleCollider.GetComponent<CollisionEventDispatcher>();
            collisionEvents.AddListenerCollisionStart(OnWallToppled);
            walls.Add(toppleCollider, instantiatedWall);
        }
    }

    private void OnWallToppled(GameObject toppleDetector)
    {
        GameObject wallObject = walls[toppleDetector];
        DeSpawnNetworkObjectFromScript(wallObject);
        walls.Remove(toppleDetector);
    }

    private bool OnDeSpawnNetworkObject(object cookie)
    {
        GameObject netGameObj = (GameObject)cookie;
        DeSpawnNetworkObjectFromScript(netGameObj);
        return true;
    }

    private void DeSpawnNetworkObjectFromScript(GameObject netGameObj)
    {
        NetworkObject netObj = netGameObj.GetComponent<NetworkObject>();
        netObj.Despawn();
    }

    // SERVER CALLED ONLY
    private void OnSnowPickedUp(Transform pickup, PlayerEntity player)
    {
        // player.SetPlayerSnowCountServerRpc(player.SnowCount.Value + 1);
        SnowPile pileData = pickup.GetComponent<SnowPile>();
        player.SetPlayerSnowCountClientRpc(pileData.Type, player.GetInventoryForType(pileData.Type).Quantity + 1);
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
                // float xVal = LocalPlayer.transform.position.x;
                float yVal = Random.Range(volume.bounds.min.y, volume.bounds.max.y);
                float zVal = Random.Range(volume.bounds.min.z, volume.bounds.max.z);
                // float zVal = LocalPlayer.transform.position.z;
                spawnPositions.Add(new Vector3(xVal, yVal, zVal));
            }
        }
        SpawnSnowballsClientRpc(spawnPositions.ToArray());
    }

    private bool OnSnowballsSpanwedFromScript(object cookie)
    {
        List<SnowballSpawnData> spawnData = (List<SnowballSpawnData>)cookie;
        for (int i = 0, count = spawnData.Count; i < count; ++i)
        {
            SnowballSpawnData data = spawnData[i];
            Vector3[] spawnPositions = data.SpawnPositions.ToArray();
            SpawnSnowballsClientRpc(spawnPositions, data.Type);
        }
        return false;
    }

    [Rpc(SendTo.Everyone)]
    public void SpawnSnowballsClientRpc(Vector3[] spawnPositions, SnowballType type = SnowballType.Basic)
    {
        for (int i = 0, count = spawnPositions.Length; i < count; ++i)
        {
            Vector3 position = spawnPositions[i];
            // Debug.Log("Locally firing projectile!");
            ThrowableObject spawnType = Constants.SnowballTypes.ThrowableObjects.Find(item => item.Type == type);
            GameObject projectileObj = Instantiate(Resources.Load<GameObject>(spawnType.PrefabName));
            projectileObj.transform.position = position;

            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            rb.position = position;
            rb.rotation = Quaternion.identity;
            LocalProjectlie projComp = projectileObj.GetComponent<LocalProjectlie>();
            projComp.SetOwner(null, IsServer);
        }
    }

    private bool OnPlaySoundEffect(object cookie)
    {
        string soundResource = (string)cookie;
        Debug.Log($"Play audio: {soundResource}");
        soundEffectPlayer.PlayOneShot(Resources.Load<AudioClip>(soundResource));
        return true;
    }

    private bool OnNetworkAction(object cookie)
    {
        int networkActionId = Service.NetworkActions.GetIndexForAction((CustomNetworkAction)cookie);
        TriggerNetworkActionForAllPlayersRpc(networkActionId);
        return false;
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerNetworkActionForAllPlayersRpc(int actionIndex)
    {
        Service.NetworkActions.TriggerNetworkAction(actionIndex);
    }
}
