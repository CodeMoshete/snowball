public class ComparePlayerTeamNameAction : CustomNetworkAction
{
    public PlayerEntityProvider Player;
    public string TeamName;
    public CustomAction OnSameTeam;
    public CustomAction OnDifferentTeam;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = Player.GetPlayerEntity();
        if (player.TeamName.Value.ToString() == TeamName)
        {
            if (OnSameTeam != null)
            {
                OnSameTeam.Initiate();
            }
        }
        else
        {
            if (OnDifferentTeam != null)
            {
                OnDifferentTeam.Initiate();
            }
        }

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
