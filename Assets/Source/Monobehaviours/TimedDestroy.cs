using UnityEngine;
using UnityEngine.Events;

public class TimedDestroy : MonoBehaviour
{
    public float Delay;
    public UnityEvent<Transform> OnDestroyed;

	void Update ()
    {
        Delay -= Time.deltaTime;
        if (Delay <= 0f)
        {
            if (OnDestroyed != null)
                OnDestroyed.Invoke(transform);

            Destroy(gameObject);
        }
	}
}
