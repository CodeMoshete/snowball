using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class Engine : MonoBehaviour
{
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
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        GameObject gameManagerObj = GameObject.Find("GameManager");
        GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
        if (startData.IsHost)
        {
            gameManager.StartHost(startData);
            // NetworkManager.Singleton.StartHost();
        }
        else 
        {
            gameManager.StartClient(startData);
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
