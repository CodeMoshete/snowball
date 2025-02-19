using System.Collections.Generic;
using UnityEngine;

public class LocalProjectlie : MonoBehaviour
{
    private const string IMPACT_EFFECT_RESOURCE = "Effects/SnowballImpactEffect";
    private const string MINOR_IMPACT_EFFECT_RESOURCE = "Effects/SnowballMinorImpactEffect";
    private const string PLAYER_TAG = "Player";
    private const string FLOOR_TAG = "Floor";
    private const string CHASM_TAG = "Chasm";
    private const string OBJECTIVE_TAG = "ProjectileObjective";
    private Rigidbody rigidBody;
    private Transform owner;
    private PlayerEntity ownerPlayer;
    private string ownerTeamName;
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
            CHASM_TAG,
            OBJECTIVE_TAG
        };
        rigidBody = GetComponent<Rigidbody>();
        gameManager = GameObject.Find(Constants.GAME_MANAGER_NAME).GetComponent<GameManager>();
    }

    public void SetOwner(Transform owner, bool isServer)
    {
        this.owner = owner;
        if (owner != null)
        {
            ownerPlayer = owner.GetComponent<PlayerEntity>();
            ownerTeamName = ownerPlayer.TeamName.Value.ToString();
        }
        ownerTeamName = "None";
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
                if (otherPlayer != null && !otherPlayer.IsFrozen && (otherPlayer.TeamName.Value != ownerTeamName || Constants.IS_FRIENDLY_FIRE_ON))
                {
                    Debug.Log("Collide with player " + otherPlayer.name + "(" + otherPlayer.TeamName.Value + ")");
                    long ownerClientId = ownerPlayer != null ? (long)ownerPlayer.OwnerClientId : -1;
                    gameManager.TransmitProjectileHitClientRpc(ownerClientId, otherPlayer.OwnerClientId);
                }
                else if(collision.gameObject.tag == FLOOR_TAG)
                {
                    gameManager.ProjectileHitFloorServerRpc(contactPt.point);
                }
                else if (collision.gameObject.tag == OBJECTIVE_TAG)
                {
                    Debug.Log("Projectile hit an objective!");
                    long ownerClientId = ownerPlayer != null ? (long)ownerPlayer.OwnerClientId : -1;
                    gameManager.ProjectileHitObjectiveServerRpc(ownerClientId, collision.gameObject.name);
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

    public void TriggerTimedDespawnAnimation(Transform despawningTransform)
    {
        Transform impactEffect = Instantiate(Resources.Load<GameObject>(MINOR_IMPACT_EFFECT_RESOURCE)).transform;
        impactEffect.position = despawningTransform.position;
    }
}
