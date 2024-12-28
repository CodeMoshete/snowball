using System.Collections.Generic;
using Unity.Android.Gradle;
using Unity.Netcode;
using UnityEditor.MPE;
using UnityEngine;
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
    private const float SNOWBALL_THROW_SPEED = 670.82f;
    private const float MIN_THROW_ANGLE = 20f;
    private const float MAX_THROW_ANGLE = 70f;

    private GameStartData startData;
    private GameObject levelPrefab;
    private Dictionary<string, List<Transform>> spawnPoints = new Dictionary<string, List<Transform>>();
    private Dictionary<string, List<ulong>> teamRosters = new Dictionary<string, List<ulong>>();
    private Dictionary<ulong, Transform> playerTransforms = new Dictionary<ulong, Transform>();
    private Dictionary<string, Transform> teamQueens = new Dictionary<string, Transform>();
    private PickupSystem pickupSystem;
    private List<BoxCollider> spawnVolumes;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameManager: OnNetworkSpawn");
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

            // Populate snowball spawn volumes
            spawnVolumes = UnityUtils.FindAllComponentsInChildren<BoxCollider>(levelPrefab);
            SpawnSnowballs(5);
        }

        GetGameMetadataServerRpc(NetworkManager.LocalClientId);
    }

    // Entry point from Menu scene - triggers the network connection to be made.
    public void StartClient(GameStartData startData)
    {
        this.startData = startData;
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost(GameStartData startData)
    {
        this.startData = startData;
        NetworkManager.Singleton.StartHost();
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
        teamQueens[startData.PlayerTeamName] = playerTransforms[startData.TeamQueenPlayerId];
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
                teamRosters.Add(teamName, new List<ulong>());
                if (!teamQueens.ContainsKey(teamName))
                {
                    teamQueens.Add(teamName, null);
                }
            }
        }
        Service.EventManager.SendEvent(EventId.LevelLoadCompleted, startData);
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

        float throwAngle = Mathf.Lerp(MIN_THROW_ANGLE, MAX_THROW_ANGLE, verticalVel) * Mathf.Deg2Rad;
        float verticalSpeed = SNOWBALL_THROW_SPEED * Mathf.Sin(throwAngle);
        float horizontalSpeed = SNOWBALL_THROW_SPEED * Mathf.Cos(throwAngle);
        rb.AddForce(new Vector3(fwd.x * horizontalSpeed, verticalSpeed, fwd.z * horizontalSpeed));
    }

    [Rpc(SendTo.Everyone)]
    public void TransmitProjectileHitClientRpc(ulong hitPlayerId)
    {
        PlayerEntity hitPlayer = playerTransforms[hitPlayerId].GetComponent<PlayerEntity>();
        hitPlayer.OnPlayerFrozen();
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
    public void RegisterPlayer(ulong playerId, PlayerEntity player)
    {
        Debug.Log("Registering player " + playerId);
        playerTransforms.Add(playerId, player.transform);
        
        if (!IsHost && player.CurrentPlayerClass.Value == PlayerClass.Queen)
        {
            Debug.Log($"Setting {player.TeamName.Value.ToString()} queen to {player.name}");
            teamQueens[player.TeamName.Value.ToString()] = player.transform;
        }
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

    [Rpc(SendTo.Server)]
    public void SpawnWallServerRpc(Vector3 position, Vector3 euler, ulong ownerId)
    {
        Transform playerTransform = playerTransforms[ownerId];
        PlayerEntity player = playerTransform.GetComponent<PlayerEntity>();
        if (player.SnowCount.Value < Constants.WALL_COST)
        {
            Debug.Log($"Player {ownerId}: Not enough Snowballs to make a wall!");
            return;
        }
        player.SetPlayerSnowCountServerRpc(player.SnowCount.Value - Constants.WALL_COST);

        GameObject instantiatedWall = Instantiate(Resources.Load<GameObject>(WALL_RESOURCE));
        NetworkObject netObj = instantiatedWall.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        instantiatedWall.transform.position = position;
        instantiatedWall.transform.eulerAngles = euler;
        Rigidbody rigidBody = instantiatedWall.GetComponent<Rigidbody>();
        rigidBody.position = position;
        rigidBody.rotation = Quaternion.Euler(euler);
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
