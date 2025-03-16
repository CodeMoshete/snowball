public class CheckPlayerEntityExistsAction : CustomNetworkAction
{
    public PlayerEntityProvider PlayerEntity;
    public CustomAction OnExists;
    public CustomAction OnDoesNotExist;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        PlayerEntity player = PlayerEntity.GetPlayerEntity();
        if (OnExists != null && player != null)
        {
            OnExists.Initiate();
        }
        else if (OnDoesNotExist != null && player == null)
        {
            OnDoesNotExist.Initiate();
        }
    }
}
