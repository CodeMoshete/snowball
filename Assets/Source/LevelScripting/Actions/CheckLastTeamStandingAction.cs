using System.Collections.Generic;
using UnityEngine;

public class CheckLastTeamStandingAction : CustomActionStringProvider
{
    public CustomAction OnLastTeamStanding;
    public CustomAction OnMultipleTeamsStanding;
    public CustomAction OnNoTeamsStanding;

    private string winningTeamName;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        List<string> survivingTeams = new List<string>();
        GameManager gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
        Dictionary<string, List<PlayerEntity>> teams = gameManager.GetTeamRosters();
        foreach (KeyValuePair<string, List<PlayerEntity>> roster in teams)
        {
            Debug.Log($"Checking team {roster.Key} surviving");
            int teamCount = roster.Value.Count;
            for (int i = 0; i < teamCount; ++i)
            {
                if (roster.Key == Constants.TEAM_UNASSIGNED)
                    continue;

                PlayerEntity entity = roster.Value[i];
                if (!entity.IsFrozen)
                {
                    survivingTeams.Add(roster.Key);
                    break;
                }
            }
        }

        Debug.Log($"{survivingTeams.Count} teams remain!");

        if (survivingTeams.Count == 1 && OnLastTeamStanding != null)
        {
            winningTeamName = survivingTeams[0];
            OnLastTeamStanding.Initiate();
        }
        else if (survivingTeams.Count == 0 && OnNoTeamsStanding != null)
        {
            OnNoTeamsStanding.Initiate();
        }
        else if (survivingTeams.Count > 1 && OnMultipleTeamsStanding != null)
        {
            OnMultipleTeamsStanding.Initiate();
        }
    }

    public override string GetStringValue()
    {
        return winningTeamName;
    }
}
