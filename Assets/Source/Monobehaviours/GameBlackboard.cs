using System.Collections.Generic;
using UnityEngine;

/* 
 * GameBlackboard is a singleton that stores global game state and data.
 * It is used to store data that needs to be accessed by multiple game objects.
 */
public class GameBlackboard : MonoBehaviour
{
    public static GameBlackboard Instance { get; private set; }
    private Dictionary<string, string> stringBlackboard = new Dictionary<string, string>();
    private Dictionary<string, int> intBlackboard = new Dictionary<string, int>();
    private Dictionary<string, bool> boolBlackboard = new Dictionary<string, bool>();

    private void Start()
    {
        Instance = this;
    }

    public void SetString(string key, string value)
    {
        stringBlackboard[key] = value;
    }

    public string GetString(string key)
    {
        if (!stringBlackboard.ContainsKey(key))
        {
            return "";
        }
        return stringBlackboard[key];
    }

    public void SetInt(string key, int value)
    {
        intBlackboard[key] = value;
    }

    public int GetInt(string key)
    {
        if (!intBlackboard.ContainsKey(key))
        {
            return 0;
        }
        return intBlackboard[key];
    }

    public void SetBool(string key, bool value)
    {
        boolBlackboard[key] = value;
    }

    public bool GetBool(string key)
    {
        if (!boolBlackboard.ContainsKey(key))
        {
            return false;
        }
        return boolBlackboard[key];
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}
