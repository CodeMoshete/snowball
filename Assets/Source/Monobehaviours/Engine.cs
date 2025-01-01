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

        if (startData.IsHost)
        {
            GameObject gameManagerObj = Instantiate(Resources.Load<GameObject>("GameManager"));
            NetworkObject gameManagerNw = gameManagerObj.GetComponent<NetworkObject>();
            GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            gameManager.StartHost(startData);
            gameManagerNw.Spawn(true);
            // GameObject gameManagerObj = GameObject.Find("GameManager");
            // NetworkManager.Singleton.StartHost();
        }
        else 
        {
            // GameObject gameManagerObj = GameObject.Find("GameManager(Clone)");
            // GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
            // gameManager.StartClient(startData);
            // NetworkManager.Singleton.StartClient();
        }

        Debug.Log("Scene Transition Done!");
    }

    public void EndGame()
    {
        gameObject.name = "Engine_Stale";
        StartCoroutine(LoadMenuSceneAsync());
    }

    IEnumerator LoadMenuSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MenuScene");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Scene Transition Done!");
        Destroy(gameObject);
    }
}
