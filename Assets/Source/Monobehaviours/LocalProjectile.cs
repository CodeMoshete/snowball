using UnityEngine;

public class LocalProjectlie : MonoBehaviour
{
    private Rigidbody rigidBody;
    private Transform owner;
    private bool isServer;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void SetOwner(Transform owner, bool isServer)
    {
        this.owner = owner;
        this.isServer = isServer;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (isServer && collision.transform != owner)
        {
            // Trigger hit
            Debug.Log("Server collision with " + collision.transform.name);
        }
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        Destroy(gameObject);
    }
}
