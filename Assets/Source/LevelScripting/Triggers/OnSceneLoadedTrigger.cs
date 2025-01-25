using UnityEngine;

public class OnSceneLoadedTrigger : MonoBehaviour
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
