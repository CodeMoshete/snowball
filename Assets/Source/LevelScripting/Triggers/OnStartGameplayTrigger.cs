using UnityEngine;

public class OnStartGameplayTrigger : MonoBehaviour
{
    public CustomAction OnTriggered;
    private void Start()
    {
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
    }

    private bool OnGameStateChanged(object cookie)
    {
        Debug.Log($"Game state changed to {(GameState)cookie}");
        if (OnTriggered != null && (GameState)cookie == GameState.Gameplay)
        {
            Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
            OnTriggered.Initiate();
        }
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
    }
}
