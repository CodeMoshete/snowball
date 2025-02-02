using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // private const string TEST_ARENA_RESOURCE = "Levels/TestArena/TestArenaPrefab";
    private const string TEST_ARENA_RESOURCE = "TestArena2";
    public TMP_InputField SessionNameField;
    public Button HostButton;
    public Button JoinButton;
    public Button QuitButton;
    // public Button NetworkJoinButton;
    // public Button NetworkCreateButton;
    public GameObject CreateSessionContainer;
    public GameObject JoinSessionContainer;
    
    public Button CreateMenuButton;
    public Button JoinMenuButton;

    public GameObject MainMenuPanel;
    public JoinGamePanel JoinGamePanel;
    public HostGamePanel HostGamePanel;

    private Engine engine;

    private bool initialized;

    public void Start()
    {
        engine = GameObject.Find("Engine").GetComponent<Engine>();
        HostButton.onClick.AddListener(OnHostClicked);
        JoinButton.onClick.AddListener(OnJoinClicked);
        QuitButton.onClick.AddListener(OnQuitClicked);
        
        CreateSessionContainer.SetActive(!Constants.IS_OFFLINE_DEBUG);
        JoinSessionContainer.SetActive(!Constants.IS_OFFLINE_DEBUG);
        HostButton.gameObject.SetActive(Constants.IS_OFFLINE_DEBUG);
        JoinButton.gameObject.SetActive(Constants.IS_OFFLINE_DEBUG);

        // NetworkJoinButton.enabled = true;
        // NetworkCreateButton.enabled = true;
        
        JoinMenuButton.onClick.AddListener(ShowJoinPanel);
        CreateMenuButton.onClick.AddListener(ShowCreatePanel);

        JoinGamePanel.Initialize(ShowMainMenu);
        HostGamePanel.Initialize(ShowMainMenu);
    }

    private void ShowMainMenu()
    {
        MainMenuPanel.SetActive(true);
    }

    private void ShowJoinPanel()
    {
        MainMenuPanel.SetActive(false);
        JoinGamePanel.gameObject.SetActive(true);
    }

    private void ShowCreatePanel()
    {
        MainMenuPanel.SetActive(false);
        HostGamePanel.ShowPanel();
    }

    public void OnHostClicked()
    {
        if (initialized)
            return;
        
        Debug.Log("Hosting session");
        initialized = true;
        GameStartData gameData = new GameStartData();
        gameData.IsHost = true;
        gameData.SessionName = SessionNameField.text;
        gameData.LevelName = HostGamePanel.SelectedLevel;
        engine.StartGame(gameData);
    }

    public void OnJoinClicked()
    {
        if (initialized)
            return;
        
        Debug.Log("Joining session");
        initialized = true;
        GameStartData gameData = new GameStartData();
        gameData.IsHost = false;
        engine.StartGame(gameData);
    }

    public void OnJoinedGame()
    {
        if (initialized)
            return;

        initialized = true;

        if (NetworkManager.Singleton.IsHost)
        {
            string selectedLevel = HostGamePanel.SelectedLevel;
            Debug.Log($"Hosting session on level {selectedLevel}");
            GameStartData gameData = new GameStartData();
            gameData.IsHost = true;
            gameData.LevelName = selectedLevel;
            gameData.SessionName = SessionNameField.text;
            engine.StartGame(gameData);
        }
        else
        {
            Debug.Log("Joining session");
            GameStartData gameData = new GameStartData();
            gameData.IsHost = false;
            engine.StartGame(gameData);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
