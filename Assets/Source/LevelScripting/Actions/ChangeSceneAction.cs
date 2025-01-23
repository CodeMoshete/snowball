using UnityEngine.SceneManagement;
using UnityEngine;

public class ChangeSceneAction : CustomNetworkAction
{
    public string NextSceneName;

    public override void Initiate()
    {
        base.Initiate();
    }
    
    public override void InitiateFromNetwork()
    {
        Debug.Log("Changing Scene: " + NextSceneName);
        SceneManager.LoadScene(NextSceneName);
    }


}
