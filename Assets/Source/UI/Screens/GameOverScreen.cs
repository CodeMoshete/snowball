using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    public TMP_Text TeamNameField;
    public Button QuitGameButton;

    private void Start()
    {
        QuitGameButton.onClick.AddListener(OnQuitPressed);
        Service.EventManager.AddListener(EventId.OnGameOver, OnGameOver);
        gameObject.SetActive(false);
    }

    private bool OnGameOver(object cookie)
    {
        gameObject.SetActive(true);
        string teamName = (string)cookie;
        TeamNameField.text = $"{teamName} wins!";
        return false;
    }

    public void OnQuitPressed()
    {
        Service.EventManager.SendEvent(EventId.OnGameQuit, null);
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.OnGameOver, OnGameOver);
    }
}
