using Utils;

public class GetPlayerEntityInParentProvider : PlayerEntityProvider
{
    public override PlayerEntity GetPlayerEntity()
    {
        PlayerEntity player = UnityUtils.FindFirstComponentInParents<PlayerEntity>(gameObject);
        return player;
    }
}
