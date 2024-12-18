using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Utils;

public enum PlayerClass
{
    Soldier,
    Queen
}

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

    void Start()
    {
        Debug.Log("Hello World!");
    }

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

    private void SpawnPlayer(ulong clientId, SpawnInfo spawnInfo)
    {
        GameObject instantiatedPlayer = Instantiate(Resources.Load<GameObject>(PLAYER_RESOURCE));
        NetworkObject netObj = instantiatedPlayer.GetComponent<NetworkObject>();
        // playerTransforms.Add(clientId, instantiatedPlayer.transform);
        netObj.SpawnWithOwnership(clientId, true);
        teamRosters[spawnInfo.TeamName].Add(clientId);
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

    [ServerRpc(RequireOwnership = false)]
    private void GetGameMetadataServerRpc(ulong clientId)
    {
        Debug.Log("Requesting game data from server");
        SpawnInfo spawnInfo = SelectTeamAndSpawnPos();
        startData.PlayerTeamName = spawnInfo.TeamName;
        startData.PlayerStartPos = spawnInfo.SpawnPoint.position;
        startData.PlayerStartEuler = spawnInfo.SpawnPoint.eulerAngles;
        SpawnPlayer(clientId, spawnInfo);

        ClientRpcParams sendParams = new ClientRpcParams();
        sendParams.Send = new ClientRpcSendParams();
        sendParams.Send.TargetClientIds = new ulong[] { clientId };
        ReceiveGameMetadataClientRpc(startData, sendParams);
    }

    [ClientRpc]
    private void ReceiveGameMetadataClientRpc(GameStartData serverStartData, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("Received start data, level: " + serverStartData.LevelName);
        startData.LevelName = serverStartData.LevelName;
        startData.PlayerStartPos = serverStartData.PlayerStartPos;
        startData.PlayerStartEuler = serverStartData.PlayerStartEuler;
        startData.PlayerTeamName = serverStartData.PlayerTeamName;
        LoadLevel();
    }

    public void PlacePlayerAtSpawn(ClientControls instantiatedPlayer)
    {
        instantiatedPlayer.transform.position = startData.PlayerStartPos;
        instantiatedPlayer.transform.eulerAngles = startData.PlayerStartEuler;
        Rigidbody rigidBody = instantiatedPlayer.GetComponent<Rigidbody>();
        rigidBody.position = startData.PlayerStartPos;
        rigidBody.rotation = Quaternion.Euler(startData.PlayerStartEuler);
        Debug.Log("Placed player " + instantiatedPlayer.OwnerClientId + " at " + startData.PlayerStartPos.ToString() + " <> " + instantiatedPlayer.transform.position.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void FireProjectileServerRpc(Vector3 position, Vector3 euler, Vector3 fwd, ulong ownerId)
    {
        // Previous server-object spawned approach
        // NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(
        //     Constants.SNOWBALL_PREFAB_NAME, position, Quaternion.Euler(euler));

        // Rigidbody rb = projectile.GetComponent<Rigidbody>();
        // rb.position = position;
        // rb.rotation = Quaternion.identity;
        // Projectile projComp = projectile.GetComponent<Projectile>();
        // projComp.SetOwner(playerTransforms[ownerId]);
        // projectile.Spawn();

        // float forceMultiplier = 600f;
        // rb.AddForce(new Vector3(fwd.x * forceMultiplier, 300f, fwd.z * forceMultiplier));
        FireProjectileClientRpc(position, euler, fwd, ownerId);
    }

    [ClientRpc]
    public void FireProjectileClientRpc(Vector3 position, Vector3 euler, Vector3 fwd, ulong ownerId)
    {
        FireProjectileLocally(position, euler, fwd, ownerId);
    }

    private void FireProjectileLocally(Vector3 position, Vector3 euler, Vector3 fwd, ulong ownerId)
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

    public void RegisterPlayer(ulong playerId, Transform player)
    {
        Debug.Log("Registering player " + playerId);
        playerTransforms.Add(playerId, player);
    }
}
