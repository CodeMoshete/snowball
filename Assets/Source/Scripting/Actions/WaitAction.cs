using UnityEngine;

public class WaitAction : CustomAction
{
    public float Duration;
    public CustomAction NextAction;
    private float currentTime;
    private bool isInitialized;

    public override void Initiate()
    {
        currentTime = Duration;
        isInitialized = true;
    }

    public void Update ()
    {
        if (isInitialized)
        {
            if (currentTime <= 0)
            {
                isInitialized = false;
                NextAction.Initiate();
            }
            else
            {
                currentTime -= Time.deltaTime;
            }
        }
	}
}
