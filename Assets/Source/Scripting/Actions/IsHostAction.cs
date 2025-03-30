using UnityEngine;

public class IsHostAction : CustomAction
{
    public CustomAction OnIsHost;
    public CustomAction OnIsNotHost;

    public override void Initiate()
    {
        GameObject gameManagerObj = GameObject.Find("GameManager(Clone)");
        if (gameManagerObj == null)
        {
            Debug.LogError("[CheckGameStateAction] GameManager not found!");
            return;
        }

        GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
        if (gameManager.IsHost)
        {
            OnIsHost.Initiate();
        }
        else
        {
            OnIsNotHost.Initiate();
        }
    }
}
