using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEventDispatcher : MonoBehaviour
{
    public const string IMPACT_EFFECT_RESOURCE = "WallToppleEffect";

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

    private void TriggerCollisionEffect(string effectResource, Vector3 contactPt)
    {
        Transform impactEffect = Instantiate(Resources.Load<GameObject>(effectResource)).transform;
        impactEffect.position = contactPt;
    }

    private void OnDestroy()
    {
        TriggerCollisionEffect(IMPACT_EFFECT_RESOURCE, transform.parent.position);
        listeners = null;
    }
}
