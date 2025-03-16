using UnityEngine;

public class ComparePlayerTeamsAction : CustomNetworkAction
{
    public PlayerEntityProvider Player1;
    public PlayerEntityProvider Player2;

    public CustomAction OnSameTeam;
    public CustomAction OnDifferentTeams;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = Player1.GetPlayerEntity();
        PlayerEntity player2 = Player2.GetPlayerEntity();
        if (player == null || player2 == null)
        {
            Debug.LogError("PlayerEntityProvider.GetPlayerEntity() returned null");
            return;
        }

        if (player.TeamName.Value == player2.TeamName.Value && OnSameTeam != null)
        {
            Debug.Log("Players are on the same team");
            OnSameTeam.Initiate();
        }
        else if (player.TeamName.Value != player2.TeamName.Value && OnDifferentTeams != null)
        {
            Debug.Log("Players are on different teams");
            OnDifferentTeams.Initiate();
        }
        else
        {
            Debug.LogWarning("No actions set for same or different teams");
        }
    }
}
