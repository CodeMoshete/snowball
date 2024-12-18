using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    public float Delay;

	void Update ()
    {
        Delay -= Time.deltaTime;
        if (Delay <= 0f)
        {
            Destroy(gameObject);
        }
	}
}
