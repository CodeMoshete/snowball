using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEventDispatcher : MonoBehaviour
{
    public const string IMPACT_EFFECT_RESOURCE = "Effects/WallToppleEffect";

    private List<Action<GameObject>> collisionStartListeners = new List<Action<GameObject>>();
    private List<Action<GameObject>> collisionEndListeners = new List<Action<GameObject>>();
    
    public void AddListenerCollisionStart(Action<GameObject> listener)
    {
        collisionStartListeners.Add(listener);
    }

    public void AddListenerCollisionEnd(Action<GameObject> listener)
    {
        collisionEndListeners.Add(listener);
    }

    private void OnTriggerEnter(Collider other)
    {
        for (int i = 0, count = collisionStartListeners.Count; i < count; ++i)
        {
            collisionStartListeners[i](gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        for (int i = 0, count = collisionEndListeners.Count; i < count; ++i)
        {
            collisionEndListeners[i](gameObject);
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
        collisionStartListeners = null;
    }
}
