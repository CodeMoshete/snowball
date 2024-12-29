using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEventDispatcher : MonoBehaviour
{
    private List<Action<GameObject>> listeners = new List<Action<GameObject>>();
    public void AddListener(Action<GameObject> listener)
    {
        listeners.Add(listener);
    }

    private void OnTriggerEnter(Collider other)
    {
        for (int i = 0, count = listeners.Count; i < count; ++i)
        {
            listeners[i](gameObject);
        }
    }

    private void OnDestroy()
    {
        listeners = null;
    }
}
