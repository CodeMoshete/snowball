using UnityEngine;

public class GameEndAction : CustomNetworkAction
{
    public CustomActionStringProvider WinningTeam;
    public CustomAction OnComplete;
    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        Debug.Log($"Game Over! {WinningTeam.GetStringValue()} wins!");
        GameManager gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
        gameManager.TransmitGameOverRpc(WinningTeam.GetStringValue());

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
