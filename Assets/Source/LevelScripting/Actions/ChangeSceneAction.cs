using UnityEngine.SceneManagement;
using UnityEngine;

public class ChangeSceneAction : CustomAction
{
    public string NextSceneName;
    public override void Initiate()
    {
        Debug.Log("Changing Scene: " + NextSceneName);
        SceneManager.LoadScene(NextSceneName);
    }
}
