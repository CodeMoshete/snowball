using UnityEngine;

public class OnGOStartTrigger : MonoBehaviour
{
    public CustomAction NextAction;
    public void Start()
    {
        NextAction.Initiate();
    }
}
