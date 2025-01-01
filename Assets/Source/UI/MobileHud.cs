using UnityEngine;
using UnityEngine.UI;

public class MobileHud : MonoBehaviour
{
    public Button ThrowButton;
    public Button JumpButton;
    public Button WallButton;
    public Button MenuButton;

    private void Start()
    {
        ThrowButton.onClick.AddListener(OnThrowPressed);
        JumpButton.onClick.AddListener(OnJumpPressed);
        WallButton.onClick.AddListener(OnWallPressed);
        MenuButton.onClick.AddListener(OnMenuPressed);
        gameObject.SetActive(false);
    }

    private void OnThrowPressed()
    {
        Service.EventManager.SendEvent(EventId.OnThrowUIButtonPressed, null);
    }

    private void OnJumpPressed()
    {
        Service.EventManager.SendEvent(EventId.OnJumpUIButtonPressed, null);
    }

    private void OnWallPressed()
    {
        Service.EventManager.SendEvent(EventId.OnWallUIButtonPressed, null);
    }

    private void OnMenuPressed()
    {
        Service.EventManager.SendEvent(EventId.OnMenuUIButtonPressed, null);
    }
}
