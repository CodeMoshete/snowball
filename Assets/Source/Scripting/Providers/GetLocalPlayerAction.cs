using UnityEngine;

public class GetLocalPlayerAction : PlayerEntityProvider
{
    public override PlayerEntity GetPlayerEntity()
    {
        GameManager gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
        return gameManager.LocalPlayer;
    }
}
