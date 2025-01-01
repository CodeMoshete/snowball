using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private NetworkObject networkObj;
    private Rigidbody rigidBody;
    private Transform owner;
    private List<string> collisionTags;

    private void Start()
    {
        collisionTags = new List<string>
        {
            "Player",
            "Floor",
            "Chasm"
        };
        networkObj = GetComponent<NetworkObject>();
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("Spawn Projectile");
    }

    public void SetOwner(Transform owner)
    {
        this.owner = owner;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (IsServer && collision.transform != owner)
        {
            if (collisionTags.IndexOf(collision.transform.tag) >= 0)
            {
                Debug.Log("Collision with " + collision.transform.name);
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
                networkObj.Despawn(false);
                NetworkObjectPool.Singleton.ReturnNetworkObject(networkObj, Constants.SNOWBALL_PREFAB_NAME);
            }
        }
    }
}
