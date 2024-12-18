using UnityEditor.PackageManager;
using UnityEngine;

public class LocalProjectlie : MonoBehaviour
{
    private const string IMPACT_EFFECT_RESOURCE = "SnowballImpactEffect";
    private Rigidbody rigidBody;
    private Transform owner;
    private ClientControls ownerPlayer;
    private bool isServer;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void SetOwner(Transform owner, bool isServer)
    {
        this.owner = owner;
        ownerPlayer = owner.GetComponent<ClientControls>();
        this.isServer = isServer;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.transform == owner)
        {
            return;
        }

        if (isServer)
        {
            // Trigger hit
            Debug.Log("Server collision with " + collision.transform.name);
            ClientControls otherPlayer = collision.gameObject.GetComponent<ClientControls>();
            if (otherPlayer != null && (otherPlayer.TeamName.Value != ownerPlayer.TeamName.Value || Constants.IS_FRIENDLY_FIRE_ON))
            {
                Debug.Log("Collide with player " + otherPlayer.name + "(" + otherPlayer.TeamName.Value + ")");
            }
        }
        Transform impactEffect = Instantiate(Resources.Load<GameObject>(IMPACT_EFFECT_RESOURCE)).transform;
        ContactPoint contactPt = collision.GetContact(0);
        impactEffect.position = contactPt.point;
        impactEffect.transform.LookAt(impactEffect.position + contactPt.normal);
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        Destroy(gameObject);
    }
}
