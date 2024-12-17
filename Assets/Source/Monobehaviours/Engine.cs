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
        StartCoroutine(LoadYourAsyncScene(startData));
    }

    IEnumerator LoadYourAsyncScene(GameStartData startData)
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
}
