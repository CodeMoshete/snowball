using System.Collections.Generic;
using UnityEngine;

public class ThawSource : MonoBehaviour
{
    public float DefrostRange;
    private float sqrDefrostRange = -1f;
    public float SqrDefrostRange
    {
        get
        {
            if (sqrDefrostRange < 0)
            {
                sqrDefrostRange = DefrostRange * DefrostRange;
            }
            return sqrDefrostRange;
        }
    }
    public string TeamName;
    private GameManager gameManager;
    private List<PlayerEntity> playersInRange = new List<PlayerEntity>();

    private void Start()
    {
        GameObject gameManagerObj = GameObject.Find("GameManager(Clone)");
        gameManager = gameManagerObj.GetComponent<GameManager>();
    }

    private void Update()
    {
        if (gameManager.Teams.ContainsKey(TeamName))
        {
            List<PlayerEntity> teamPlayers = gameManager.Teams[TeamName];
            for (int i = 0, count = teamPlayers.Count; i < count; ++i)
            {
                PlayerEntity player = teamPlayers[i];
                bool isInRange = Vector3.SqrMagnitude(player.transform.position - transform.position) < SqrDefrostRange;
                if (!playersInRange.Contains(player) && isInRange)
                {
                    playersInRange.Add(player);
                    player.SetThawSourceInRange(this);
                }
                else if (playersInRange.Contains(player) && !isInRange)
                {
                    playersInRange.Remove(player);
                    player.RemoveThawSourceInRange(this);
                }
            }
        }
    }
}
