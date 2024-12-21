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
    public const int BULLETS_TO_SPAWN = 30;
    public const string PROJECTILE_RESOURCE = "Snowball";
    public const string PLAYER_RESOURCE = "PlayerPrefab";
    private const string CAMERA_NAME = "Main Camera";

    private GameStartData startData;
    private GameObject levelPrefab;
    private Dictionary<string, List<Transform>> spawnPoints = new Dictionary<string, List<Transform>>();
    private Dictionary<string, List<ulong>> teamRosters = new Dictionary<string, List<ulong>>();
    private Dictionary<ulong, Transform> playerTransforms = new Dictionary<ulong, Transform>();
    private Dictionary<string, Transform> teamQueens = new Dictionary<string, Transform>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameManager: OnNetworkSpawn");
        if (IsServer)
        {
            Debug.Log("Server");
            levelPrefab = Instantiate(Resources.Load<GameObject>(startData.LevelName));
            GameObject spawnPointContainer = UnityUtils.FindGameObject(levelPrefab, "SpawnPoints");
            List<GameObject> teamSpawnPoints = UnityUtils.GetTopLevelChildren(spawnPointContainer);
            for (int i = 0, count = teamSpawnPoints.Count; i < count; ++i)
            {
                string teamName = teamSpawnPoints[i].name;
                List<Transform> spawnPointsTransforms = UnityUtils.GetTopLevelChildTransforms(teamSpawnPoints[i]);
                spawnPoints.Add(teamName, spawnPointsTransforms);
                teamRosters.Add(teamName, new List<ulong>());
            }
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

    public void SetUpNewPlayer(Transform newPlayer)
    {
        GameObject cameraObj = GameObject.Find(CAMERA_NAME);
        if (cameraObj != null)
        {
            cameraObj.transform.SetParent(newPlayer.transform);
            cameraObj.transform.localPosition = new Vector3(0f, 0.5f, -5f);
        }
    }

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
    [ServerRpc(RequireOwnership = false)]
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

        ClientRpcParams sendParams = new ClientRpcParams();
        sendParams.Send = new ClientRpcSendParams();
        sendParams.Send.TargetClientIds = new ulong[] { clientId };
        ReceiveGameMetadataClientRpc(startData, sendParams);
        // ReceiveGameMetadataClientRpc(startData);
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

    // Called on server only
    private void SpawnPlayer(ulong clientId, SpawnInfo spawnInfo)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);
        teamRosters[spawnInfo.TeamName].Add(clientId);
    }

    // Server message sent to all players when a new player joins the game
    [ClientRpc]
    private void ReceiveGameMetadataClientRpc(GameStartData serverStartData, ClientRpcParams clientRpcParams = default)
    {
        // Debug.Log("Received start data, level: " + serverStartData.LevelName);
        startData.LevelName = serverStartData.LevelName;
        startData.PlayerStartPos = serverStartData.PlayerStartPos;
        startData.PlayerStartEuler = serverStartData.PlayerStartEuler;
        startData.PlayerTeamName = serverStartData.PlayerTeamName;
        startData.PlayerClass = serverStartData.PlayerClass;
        LoadLevel();
    }

    [ServerRpc(RequireOwnership = false)]
    public void FireProjectileServerRpc(Vector3 position, Vector3 euler, Vector3 fwd, ulong ownerId)
    {
        FireProjectileClientRpc(position, euler, fwd, ownerId);
    }

    [ClientRpc]
    public void FireProjectileClientRpc(Vector3 position, Vector3 euler, Vector3 fwd, ulong ownerId)
    {
        Debug.Log("Locally firing projectile!");
        GameObject projectileObj = Instantiate(Resources.Load<GameObject>("LocalSnowball"));
        projectileObj.transform.position = position;
        projectileObj.transform.eulerAngles = euler;

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        rb.position = position;
        rb.rotation = Quaternion.identity;
        LocalProjectlie projComp = projectileObj.GetComponent<LocalProjectlie>();
        Transform owner = playerTransforms[ownerId];
        projComp.SetOwner(owner, IsServer);

        float forceMultiplier = 600f;
        rb.AddForce(new Vector3(fwd.x * forceMultiplier, 300f, fwd.z * forceMultiplier));
    }

    [ClientRpc]
    public void TransmitProjectileHitClientRpc(ulong hitPlayerId)
    {
        PlayerEntity hitPlayer = playerTransforms[hitPlayerId].GetComponent<PlayerEntity>();
        hitPlayer.OnPlayerFrozen();
    }

    // This gets called on each client for each player entity in the game.
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

    public Transform GetQueenForTeam(string teamName)
    {
        return teamQueens[teamName];
    }
}
