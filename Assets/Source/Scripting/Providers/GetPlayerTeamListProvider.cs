using System.Collections.Generic;
using UnityEngine;

public class GetPlayerTeamListProvider : PlayerEntityListProvider
{
    public string TeamName;

    public override List<PlayerEntity> GetPlayerEntities()
    {
        List<PlayerEntity> playerEntities = new List<PlayerEntity>();

        GameObject gameManagerObj = GameObject.Find("GameManager(Clone)");
        if (gameManagerObj == null)
        {
            Debug.LogError("[CheckGameStateAction] GameManager not found!");
            return playerEntities;
        }

        GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
        Dictionary<string, List<PlayerEntity>> teams = gameManager.GetTeamRosters();

        if (!teams.TryGetValue(TeamName, out playerEntities))
        {
            Debug.LogError($"[IterateOverTeamAction]: Team '{TeamName}' not found!");
        }

        return playerEntities;
    }
}
