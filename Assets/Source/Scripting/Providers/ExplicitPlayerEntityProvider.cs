public class ExplicitPlayerEntityProvider : PlayerEntityProvider
{
    public PlayerEntity Player;
    public override PlayerEntity GetPlayerEntity()
    {
        return Player;
    }
}
