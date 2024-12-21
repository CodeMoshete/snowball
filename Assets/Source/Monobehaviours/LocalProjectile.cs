using UnityEditor.PackageManager;
using UnityEngine;

public class LocalProjectlie : MonoBehaviour
{
    private const string IMPACT_EFFECT_RESOURCE = "SnowballImpactEffect";
    private Rigidbody rigidBody;
    private Transform owner;
    private PlayerEntity ownerPlayer;
    private GameManager gameManager;
    private bool isServer;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void SetOwner(Transform owner, bool isServer)
    {
        this.owner = owner;
        ownerPlayer = owner.GetComponent<PlayerEntity>();
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
            Debug.Log("Server collision with " + collision.transform.name);
            PlayerEntity otherPlayer = collision.gameObject.GetComponent<PlayerEntity>();
            if (otherPlayer != null && !otherPlayer.IsFrozen && (otherPlayer.TeamName.Value != ownerPlayer.TeamName.Value || Constants.IS_FRIENDLY_FIRE_ON))
            {
                Debug.Log("Collide with player " + otherPlayer.name + "(" + otherPlayer.TeamName.Value + ")");
                gameManager.TransmitProjectileHitClientRpc(otherPlayer.OwnerClientId);
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
