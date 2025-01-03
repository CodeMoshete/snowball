using UnityEngine;

public class OnSceneLoadedAction : MonoBehaviour
{
    public CustomAction NextAction;

    public void Start()
    {
        if (NextAction != null)
        {
            NextAction.Initiate();
        }
    }
}
