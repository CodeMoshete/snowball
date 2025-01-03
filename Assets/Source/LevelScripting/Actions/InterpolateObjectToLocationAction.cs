using UnityEngine;

public class InterpolateObjectToLocationAction : CustomAction
{
    public float Delay;
    public GameObject TargetObject;
	public Transform TargetPosition;
    public bool EaseIn;
    public bool EaseOut;
	public float MovementDuration;
    public CustomAction OnStart;
    public CustomAction OnComplete;
    private float currentDuration;
    private Vector3 playerStartPos;

	private bool isActive;

	public override void Initiate()
	{
		isActive = true;
        currentDuration = MovementDuration;
        playerStartPos = TargetObject.transform.position;

        if (OnStart != null)
        {
            OnStart.Initiate();
        }
	}
	
	void Update ()
    {
		if(isActive)
		{
            float dt = Time.deltaTime;
            if (Delay > 0f)
            {
                Delay -= dt;
                return;
            }

            currentDuration -= dt;
            float pct = 1 - currentDuration / MovementDuration;
            ApplyEase(ref pct);
            Vector3 nextPos = Vector3.Lerp(playerStartPos, TargetPosition.transform.position, pct);
            TargetObject.transform.position = nextPos;

            if (currentDuration <= 0f)
            {
                FinishMovement();
            }
		}
	}

    void ApplyEase(ref float pct)
    {
        if (EaseIn && EaseOut)
        {
            if (pct > 0.5f)
            {
                pct = -(2f * Mathf.Pow(pct - 1f, 2f)) + 1;
            }
            else
            {
                pct = 2f * Mathf.Pow(pct, 2f);
            }
        }
        else if (EaseIn)
        {
            pct = pct * pct;
        }
        else if (EaseOut)
        {
            pct = -(Mathf.Pow(pct - 1f, 2f)) + 1f;
        }
    }

    private void FinishMovement()
    {
        TargetObject.transform.position = TargetPosition.transform.position;
        isActive = false;

        if (OnComplete != null)
        {
            OnComplete.Initiate();
        }
    }
}
