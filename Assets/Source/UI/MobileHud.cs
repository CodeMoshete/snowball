using UnityEngine;
using UnityEngine.UI;

public class MobileHud : MonoBehaviour
{
    public Button ThrowButton;
    public Button JumpButton;
    public Button WallButton;
    public Button MenuButton;
    public GameObject WallTypeContainer;
    public Button NextWallButton;
    public Button PrevWallButton;

    private void Start()
    {
        ThrowButton.onClick.AddListener(OnThrowPressed);
        JumpButton.onClick.AddListener(OnJumpPressed);
        WallButton.onClick.AddListener(OnWallPressed);
        MenuButton.onClick.AddListener(OnMenuPressed);
        NextWallButton.onClick.AddListener(OnNextWallPressed);
        PrevWallButton.onClick.AddListener(OnPrevWallPressed);
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

    private void OnNextWallPressed()
    {
        Service.EventManager.SendEvent(EventId.OnNextWallUIButtonPressed, null);
    }

    private void OnPrevWallPressed()
    {
        Service.EventManager.SendEvent(EventId.OnPrevWallUIButtonPressed, null);
    }

    private void OnMenuPressed()
    {
        Service.EventManager.SendEvent(EventId.OnMenuUIButtonPressed, null);
    }
}
