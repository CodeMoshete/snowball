using Unity.Netcode;
using UnityEngine;

public struct GameStartData : INetworkSerializable
{
    public bool IsHost;
    public string LevelName;
    public string PlayerName;
    public string PlayerTeamName;
    public Vector3 PlayerStartPos;
    public Vector3 PlayerStartEuler;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LevelName);
        serializer.SerializeValue(ref PlayerTeamName);
        serializer.SerializeValue(ref PlayerStartPos);
        serializer.SerializeValue(ref PlayerStartEuler);
    }
}
