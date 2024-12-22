using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class LocalProjectlie : MonoBehaviour
{
    private const string IMPACT_EFFECT_RESOURCE = "SnowballImpactEffect";
    private const string MINOR_IMPACT_EFFECT_RESOURCE = "SnowballMinorImpactEffect";
    private const string PLAYER_TAG = "Player";
    private const string FLOOR_TAG = "Floor";
    private const string CHASM_TAG = "Chasm";
    private Rigidbody rigidBody;
    private Transform owner;
    private PlayerEntity ownerPlayer;
    private GameManager gameManager;
    private List<string> collisionTags;
    private bool isServer;
    private bool isMinorCollisionUsed;

    private void Start()
    {
        collisionTags = new List<string>
        {
            PLAYER_TAG,
            FLOOR_TAG,
            CHASM_TAG
        };
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

        if (collisionTags.IndexOf(collision.transform.tag) >= 0)
        {
            ContactPoint contactPt = collision.GetContact(0);
            if (isServer)
            {
                Debug.Log("Server collision with " + collision.transform.name);
                PlayerEntity otherPlayer = collision.gameObject.GetComponent<PlayerEntity>();
                if (otherPlayer != null && !otherPlayer.IsFrozen && (otherPlayer.TeamName.Value != ownerPlayer.TeamName.Value || Constants.IS_FRIENDLY_FIRE_ON))
                {
                    Debug.Log("Collide with player " + otherPlayer.name + "(" + otherPlayer.TeamName.Value + ")");
                    gameManager.TransmitProjectileHitClientRpc(otherPlayer.OwnerClientId);
                }
                else if(collision.gameObject.tag == FLOOR_TAG)
                {
                    gameManager.ProjectileHitFloorServerRpc(contactPt.point);
                }
            }
            TriggerCollisionEffect(IMPACT_EFFECT_RESOURCE, contactPt);
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            Destroy(gameObject);
        }
        else if (!isMinorCollisionUsed)
        {
            TriggerCollisionEffect(MINOR_IMPACT_EFFECT_RESOURCE, collision.GetContact(0));
        }
    }

    private void TriggerCollisionEffect(string effectResource, ContactPoint contactPt)
    {
        Transform impactEffect = Instantiate(Resources.Load<GameObject>(effectResource)).transform;
        impactEffect.position = contactPt.point;
        impactEffect.transform.LookAt(impactEffect.position + contactPt.normal);
    }
}
