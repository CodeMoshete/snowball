using System;
using System.Collections.Generic;

public enum FloatDataStream
{
    PlayerHealth
}

// Similar to EventManager, but for callbacks that will need to be called every frame.
// This way, we can avoid the overhead of having to unbox and cast event cookies every frame.
public class DataStreamManager
{
    private Dictionary<FloatDataStream, List<Action<float>>> floatDataStreams;

    public DataStreamManager()
    {
        floatDataStreams = new Dictionary<FloatDataStream, List<Action<float>>>();
    }

    public void AddFloatDataStreamListener(FloatDataStream streamType, Action<float> callback)
    {
        if (!floatDataStreams.ContainsKey(streamType))
        {
            floatDataStreams.Add(streamType, new List<Action<float>>());
        }
        floatDataStreams[streamType].Add(callback);
    }

    public void RemoveFloatDataStreamListener(FloatDataStream streamType, Action<float> callback)
    {
        if (floatDataStreams.ContainsKey(streamType))
        {
            floatDataStreams[streamType].Remove(callback);
        }
    }

    public void UpdateFloatDataStream(FloatDataStream streamType, float value)
    {
        if (floatDataStreams.ContainsKey(streamType))
        {
            foreach (var callback in floatDataStreams[streamType])
            {
                callback(value);
            }
        }
    }
}
