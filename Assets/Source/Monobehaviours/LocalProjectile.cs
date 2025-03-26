using System.Collections.Generic;
using UnityEngine;
using Utils;

public class LocalProjectlie : MonoBehaviour
{
    private const string IMPACT_EFFECT_RESOURCE = "Effects/SnowballImpactEffect";
    private const string MINOR_IMPACT_EFFECT_RESOURCE = "Effects/SnowballMinorImpactEffect";
    private const string PLAYER_TAG = "Player";
    private const string FLOOR_TAG = "Floor";
    private const string CHASM_TAG = "Chasm";
    private const string OBJECTIVE_TAG = "ProjectileObjective";
    private const string HEALTH_TAG = "HealthObject";
    private Rigidbody rigidBody;
    private Transform owner;
    private PlayerEntity ownerPlayer;
    private string ownerTeamName;
    private GameManager gameManager;
    private List<string> collisionTags;
    private bool isServer;
    private bool isMinorCollisionUsed;
    
    public bool FreezePlayer;
    public bool LeaveSnowPileOnThrow;

    public SnowballType Type;
    public float DamageAmount;
    public string ImpactEffectPrefabPath = IMPACT_EFFECT_RESOURCE;
    public ExplicitPlayerEntityProvider HitPlayerProvider;
    public ExplicitPlayerEntityProvider ThrowingPlayerProvider;
    public ExplicitTransformProvider HitTransformProvider;

    public CustomAction OnHitPlayer;
    public CustomAction OnHitObjective;
    public CustomAction OnHitFloor;

    private void Start()
    {
        collisionTags = new List<string>
        {
            PLAYER_TAG,
            FLOOR_TAG,
            CHASM_TAG,
            OBJECTIVE_TAG,
            HEALTH_TAG
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
            if (HitTransformProvider != null)
                HitTransformProvider.Position = contactPt.point;

            if (isServer)
            {
                Debug.Log("Server collision with " + collision.transform.name);
                
                if (ThrowingPlayerProvider != null)
                        ThrowingPlayerProvider.Player = ownerPlayer;

                PlayerEntity otherPlayer = collision.gameObject.GetComponent<PlayerEntity>();
                if (otherPlayer != null)
                {
                    if (HitPlayerProvider != null)
                        HitPlayerProvider.Player = otherPlayer;

                    Debug.Log("Collide with player " + otherPlayer.name + "(" + otherPlayer.TeamName.Value + ")");
                    if (!otherPlayer.IsFrozen &&
                        (otherPlayer.TeamName.Value != ownerTeamName || Constants.IS_FRIENDLY_FIRE_ON) &&
                        FreezePlayer)
                    {
                        long ownerClientId = ownerPlayer != null ? (long)ownerPlayer.OwnerClientId : -1;
                        float damage = DamageAmount * Constants.DamageMultiplier;
                        gameManager.TransmitProjectileHitClientRpc(ownerClientId, otherPlayer.OwnerClientId, damage);
                    }

                    if (OnHitPlayer != null)
                        OnHitPlayer.Initiate();
                }
                else if(collision.gameObject.tag == FLOOR_TAG)
                {
                    if (LeaveSnowPileOnThrow || ownerPlayer == null)
                        gameManager.ProjectileHitFloorServerRpc(contactPt.point, Type);

                    if (OnHitFloor != null)
                    {
                        OnHitFloor.Initiate();
                    }
                }
                else if (collision.gameObject.tag == OBJECTIVE_TAG)
                {
                    Debug.Log("Projectile hit an objective!");
                    long ownerClientId = ownerPlayer != null ? (long)ownerPlayer.OwnerClientId : -1;
                    gameManager.ProjectileHitObjectiveServerRpc(ownerClientId, collision.gameObject.name);

                    if (OnHitObjective != null)
                    {
                        OnHitObjective.Initiate();
                    }
                }
                else if (collision.gameObject.tag == HEALTH_TAG)
                {
                    Debug.Log($"Projectile hit a health object: {collision.gameObject.name}");
                    ObjectHealthTrigger healthTrigger = UnityUtils.FindFirstComponentInParents<ObjectHealthTrigger>(collision.gameObject);
                    healthTrigger.OnHit();
                }
            }
            string impactEffect = ownerPlayer != null ? ImpactEffectPrefabPath : IMPACT_EFFECT_RESOURCE;
            TriggerCollisionEffect(impactEffect, contactPt);
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
