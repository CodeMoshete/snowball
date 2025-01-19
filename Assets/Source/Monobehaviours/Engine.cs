using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class Engine : MonoBehaviour
{
    public GameStartData StartData { get; private set; }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame(GameStartData startData)
    {
        StartCoroutine(LoadGameSceneAsync(startData));
    }

    IEnumerator LoadGameSceneAsync(GameStartData startData)
    {
        StartData = startData;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (Constants.IS_OFFLINE_DEBUG)
        {
            GameObject gameManagerObj = Instantiate(Resources.Load<GameObject>("GameManager"));
            GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            if (startData.IsHost)
            {
                gameManager.StartHost(startData);
            }
            else
            {
                gameManager.StartClient(startData);
            }
        }
        else if (startData.IsHost)
        {
            GameObject gameManagerObj = Instantiate(Resources.Load<GameObject>("GameManager"));
            GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            NetworkObject gameManagerNw = gameManagerObj.GetComponent<NetworkObject>();
            gameManager.StartHost(startData);
            gameManagerNw.Spawn(true);
        }

        Debug.Log("Scene Transition Done!");
    }

    public void EndGame()
    {
        StartCoroutine(LoadMenuSceneAsync());
    }

    public void StartEndSequence(UnityEngine.UI.Button.ButtonClickedEvent leaveGameEvent)
    {
        StartCoroutine(CleanUpClientSession(leaveGameEvent));
    }

    IEnumerator CleanUpClientSession(UnityEngine.UI.Button.ButtonClickedEvent leaveGameEvent)
    {
        // TERRIBLE HACK - Trigger leave game for Multiplayer Session Widgets.
        Debug.Log("Forcing player disconnect through Multiplayer Widgets!");
        yield return null; // Wait 1 frame so the stupid button click logic will actually fire.
        leaveGameEvent.Invoke(); // End the current Session within the Widget systems.
        EndGame(); // Actually end the game now that the Session is cleaned up.
    }

    IEnumerator LoadMenuSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MenuScene");

        // Wait until the asynchronous scene fully loads
        while (asyncLoad != null && !asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Scene Transition Done!");
    }
}
