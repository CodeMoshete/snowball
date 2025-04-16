using System.Collections.Generic;

public class IterateOverPlayerListAction : CustomNetworkAction
{
    public PlayerEntityListProvider PlayerList;
    public ExplicitPlayerEntityProvider PlayerIterator;
    public CustomAction OnEachPlayer;
    public CustomAction OnComplete;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        List<PlayerEntity> players = PlayerList.GetPlayerEntities();
        for (int i = 0, count = players.Count; i < count; ++i)
        {
            PlayerIterator.Player = players[i];
            OnEachPlayer?.Initiate();
        }

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
