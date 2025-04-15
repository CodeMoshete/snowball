using System.Collections.Generic;
using UnityEngine;

public class IterateOverAllPlayersAction : PlayerEntityListProvider
{
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
        Dictionary<string, List<PlayerEntity>> allTeams = gameManager.GetTeamRosters();
        foreach (var team in allTeams)
        {
            playerEntities.AddRange(team.Value);
        }

        return playerEntities;
    }
}