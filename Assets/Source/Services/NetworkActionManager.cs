using System.Collections.Generic;
using UnityEngine;
using Utils;

public class NetworkActionManager
{
    private List<CustomNetworkAction> networkActions = new List<CustomNetworkAction>();
    
    // Indices of networkActions that will be sent and run in order for late joining users.
    private List<int> actionsIndicesToSync = new List<int>();
    public int[] CurrentActionsToSync
    {
        get
        {
            return actionsIndicesToSync.ToArray();
        }
    }

    public void RegisterNetworkActionsForLevel(GameObject levelObj)
    {
        List<GameObject> networkActionObjs = UnityUtils.FindAllGameObjectContains<CustomNetworkAction>(levelObj);
        for (int i = 0, count = networkActionObjs.Count; i < count; ++i)
        {
            CustomNetworkAction action = networkActionObjs[i].GetComponent<CustomNetworkAction>();
            // Debug.Log($"[NetworkActionManager]: Adding {action.name}");
            networkActions.Add(action);
        }
    }

    public void SyncActionsForLateJoiningUser(int[] actionIndices)
    {
        for (int i = 0, count = actionIndices.Length; i < count; ++i)
        {
            networkActions[actionIndices[i]].InitiateFromNetwork();
        }
    }

    public void TriggerNetworkAction(int actionIndex)
    {
        CustomNetworkAction networkAction = networkActions[actionIndex];
        if (networkAction.IsSyncedOnJoin)
        {
            actionsIndicesToSync.Add(actionIndex);
        }
        // Debug.Log($"[NetworkActionManager]: Triggering action {networkAction.name}");
        networkAction.InitiateFromNetwork();
    }

    public int GetIndexForAction(CustomNetworkAction action)
    {
        return networkActions.IndexOf(action);
    }

    public void ClearNetworkActionsForCurrentLevel()
    {
        networkActions.Clear();
        actionsIndicesToSync.Clear();
    }
}
