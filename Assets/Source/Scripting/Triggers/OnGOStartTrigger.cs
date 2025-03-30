using UnityEngine;

public class OnGOStartTrigger : MonoBehaviour
{
    public CustomAction OnStart;
    public CustomAction OnEnabled;

    public void Start()
    {
        if (OnStart != null)
        {
            OnStart.Initiate();
        }
    }

    public void OnEnable()
    {
        if (OnEnabled != null)
        {
            OnEnabled.Initiate();
        }
    }
}
