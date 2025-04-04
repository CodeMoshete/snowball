using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    public Button ResumeButton;
    // public Button QuitButton;
    public Slider LookSpeedSlider;
    public Toggle InvertLookToggle;

    private void Start()
    {
        ResumeButton.onClick.AddListener(OnResumePressed);
        // QuitButton.onClick.AddListener(OnQuitPressed);
        LookSpeedSlider.onValueChanged.AddListener(OnLookSpeedChanged);
        InvertLookToggle.onValueChanged.AddListener(OnInvertLookToggled);
        Service.EventManager.AddListener(EventId.OnGameResume, OnGameResumed);
        Service.EventManager.AddListener(EventId.OnGamePause, OnGamePaused);
        LookSpeedSlider.value = 0.5f;
        gameObject.SetActive(false);
    }

    private bool OnGameResumed(object cookie)
    {
        gameObject.SetActive(false);
        return false;
    }

    private bool OnGamePaused(object cookie)
    {
        gameObject.SetActive(true);
        return false;
    }

    private void OnResumePressed()
    {
        Service.EventManager.SendEvent(EventId.OnGameResume, null);
    }

    // Invoked by "Leave Button" which is a Unity Multiplayer Widget in the editor / GameScene UI Canvas.
    public void OnQuitPressed()
    {
        Service.EventManager.SendEvent(EventId.OnGameQuit, null);
    }

    private void OnLookSpeedChanged(float newValue)
    {
        Service.EventManager.SendEvent(EventId.OnLookSpeedUpdated, newValue);
    }

    private void OnInvertLookToggled(bool value)
    {
        Service.EventManager.SendEvent(EventId.OnLookInvertToggled, value);
    }

    public void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.OnGameResume, OnGameResumed);
        Service.EventManager.RemoveListener(EventId.OnGamePause, OnGamePaused);
    }
}
