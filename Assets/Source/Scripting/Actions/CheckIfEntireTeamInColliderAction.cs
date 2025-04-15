using System.Collections.Generic;
using UnityEngine;

public class CheckIfEntireTeamInColliderAction : CustomNetworkAction
{
    public Collider ColliderToCheck;
    public string TeamName;
    public CustomAction OnAllIn;
    public CustomAction OnNotAllIn;

    public override void Initiate()
    {
        base.Initiate();
    }

    public override void InitiateFromNetwork()
    {
        List<PlayerEntity> playerEntities = new List<PlayerEntity>();

        if (ColliderToCheck != null)
        {
            Collider[] colliders = Physics.OverlapBox(ColliderToCheck.bounds.center, ColliderToCheck.bounds.extents, Quaternion.identity);

            foreach (Collider collider in colliders)
            {
                PlayerEntity playerEntity = collider.GetComponent<PlayerEntity>();
                if (playerEntity != null)
                {
                    playerEntities.Add(playerEntity);
                }
            }
        }

        int numPlayersInCollider = playerEntities.Count;
        if (numPlayersInCollider == 0)
        {
            ContinueNotAllIn();
            return;
        }

        GameObject gameManagerObj = GameObject.Find("GameManager(Clone)");
        if (gameManagerObj == null)
        {
            Debug.LogError("[CheckGameStateAction] GameManager not found!");
            ContinueNotAllIn();
            return;
        }

        GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
        Dictionary<string, List<PlayerEntity>> teams = gameManager.GetTeamRosters();

        if (!teams.ContainsKey(TeamName))
        {
            Debug.LogError($"[IterateOverTeamAction]: Team '{TeamName}' not found!");
            ContinueNotAllIn();
            return;
        }
        List<PlayerEntity> teamMembers = teams[TeamName];

        bool allInCollider = true;
        for (int i = 0; i < numPlayersInCollider; ++i)
        {
            PlayerEntity playerEntity = playerEntities[i];
            if (!teamMembers.Contains(playerEntity))
            {
                allInCollider = false;
                break;
            }
        }

        if (allInCollider)
        {
            if (OnAllIn != null)
            {
                OnAllIn.Initiate();
            }
        }
        else
        {
            ContinueNotAllIn();
        }
    }

    private void ContinueNotAllIn()
    {
        if (OnNotAllIn != null)
        {
            OnNotAllIn.Initiate();
        }
    }
}
