using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TMP_InputField SessionNameField;
    public Button HostButton;
    public Button JoinButton;
    public Button QuitButton;
    public GameObject CreateSessionContainer;
    public GameObject JoinSessionContainer;
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
    }

    public void OnHostClicked()
    {
        if (initialized)
            return;
        
        Debug.Log("Hosting session");
        initialized = true;
        GameStartData gameData = new GameStartData();
        gameData.IsHost = true;
        gameData.LevelName = "TestArenaPrefab";
        gameData.SessionName = SessionNameField.text;
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
            Debug.Log("Hosting session");
            GameStartData gameData = new GameStartData();
            gameData.IsHost = true;
            gameData.LevelName = "TestArenaPrefab";
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
