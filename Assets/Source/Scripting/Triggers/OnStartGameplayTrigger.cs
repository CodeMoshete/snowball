using UnityEngine;

public class OnStartGameplayTrigger : MonoBehaviour
{
    public CustomAction OnTriggered;
    private bool hasBeenTriggered;
    private void Start()
    {
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
    }

    private bool OnGameStateChanged(object cookie)
    {
        if (hasBeenTriggered)
            return false;

        Debug.Log($"Game state changed to {(GameState)cookie}");
        if (OnTriggered != null && (GameState)cookie == GameState.Gameplay)
        {
            OnTriggered.Initiate();
            hasBeenTriggered = true;
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
    }
}
