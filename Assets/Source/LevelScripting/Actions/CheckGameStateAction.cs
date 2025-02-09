using Unity.Netcode;
using UnityEngine;

public class CheckGameStateAction : CustomAction
{
    public CustomAction OnPreGame;
    public CustomAction OnGameplay;
    public CustomAction OnGameplayPaused;
    public CustomAction OnPostGame;
    public override void Initiate()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            GameObject gameManagerObj = GameObject.Find("GameManager (Clone)");
            if (gameManagerObj == null)
            {
                Debug.LogError("[CheckGameStateAction] GameManager not found!");
                return;
            }

            GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            if (OnPreGame != null && gameManager.CurrentGameState == GameState.PreGameLobby)
            {
                OnPreGame.Initiate();
            }
            else if (OnGameplay != null && gameManager.CurrentGameState == GameState.Gameplay)
            {
                OnGameplay.Initiate();
            }
            else if (OnGameplayPaused != null && gameManager.CurrentGameState == GameState.GameplayPaused)
            {
                OnGameplayPaused.Initiate();
            }
            else if (OnPostGame != null && gameManager.CurrentGameState == GameState.PostGame)
            {
                OnPostGame.Initiate();
            }
        }
    }
}
