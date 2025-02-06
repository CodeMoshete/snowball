using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct GameStartData : INetworkSerializable
{
    public string SessionName;
    public bool IsHost;
    public ulong PlayerId;
    public string LevelName;
    public string PlayerName;
    public string PlayerTeamName;
    public ulong TeamQueenPlayerId;
    public PlayerClass PlayerClass;
    public Vector3 PlayerStartPos;
    public Vector3 PlayerStartEuler;
    public GameState CurrentGameState;
    public int[] StartActions;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LevelName);
        serializer.SerializeValue(ref PlayerTeamName);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref TeamQueenPlayerId);
        serializer.SerializeValue(ref PlayerStartPos);
        serializer.SerializeValue(ref PlayerStartEuler);
        serializer.SerializeValue(ref PlayerClass);
        serializer.SerializeValue(ref CurrentGameState);
        serializer.SerializeValue(ref StartActions);
    }
}
