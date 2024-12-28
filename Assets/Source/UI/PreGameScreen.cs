using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class PreGameScreen : MonoBehaviour
{
    private const string TEAM_ROSTER_PREFAB = "UI/TeamRosterContainer";
    private const string PLAYER_CARD_PREFAB = "UI/PlayerNameCard";
    private const string TEAM_NAME_LABEL = "TeamNameLabel";
    private const string NAME_CONTAINER_OBJ = "TeamNamesContainer";
    private const string PLAYER_NAME_OBJ = "PlayerName";
    
    public Transform RosterContainer;
    public TMP_Text ClientActionLabel;
    public TMP_Text HostActionLabel;
    public Button HostStartButton;

    private List<GameObject> teamRosterObjects;

    private void Start()
    {
        teamRosterObjects = new List<GameObject>();
        HostStartButton.onClick.AddListener(TriggerStartGameplay);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.AddListener(EventId.PlayerRosterUpdated, OnPlayerRosterUpdated);
        Service.EventManager.AddListener(EventId.GameManagerInitialized, OnGameManagerInitialized);
    }

    public bool OnGameManagerInitialized(object cookie)
    {
        bool isHost = (bool)cookie;
        HostActionLabel.gameObject.SetActive(isHost);
        HostStartButton.gameObject.SetActive(isHost);
        ClientActionLabel.gameObject.SetActive(!isHost);
        return false;
    }

    private void TriggerStartGameplay()
    {
        Service.EventManager.SendEvent(EventId.StartGameplayPressed, null);
    }

    private bool OnGameStateChanged(object cookie)
    {
        GameState gameState = (GameState)cookie;
        gameObject.SetActive(gameState == GameState.PreGameLobby);
        return false;
    }

    private bool OnPlayerRosterUpdated(object cookie)
    {
        Dictionary<string, List<string>> rosters = (Dictionary<string, List<string>>)cookie;
        Debug.Log("Roster update!");

        // Clear existing roster objects
        for (int i = 0, count = teamRosterObjects.Count; i < count; ++i)
        {
            Destroy(teamRosterObjects[i]);
        }

        foreach (KeyValuePair<string, List<string>> team in rosters)
        {
            CreateTeamList(team.Key, team.Value);
        }

        return false;
    }

    private void CreateTeamList(string teamName, List<string> playerNames)
    {
        GameObject teamCard = Instantiate(Resources.Load<GameObject>(TEAM_ROSTER_PREFAB), RosterContainer);
        TMP_Text teamNameField = UnityUtils.FindGameObject(teamCard, TEAM_NAME_LABEL).GetComponent<TMP_Text>();
        teamNameField.text = teamName;
        Transform playerNameContaier = UnityUtils.FindGameObject(teamCard, NAME_CONTAINER_OBJ).transform;

        for (int i = 0, count = playerNames.Count; i < count; ++i)
        {
            GameObject playerNameCard = Instantiate(Resources.Load<GameObject>(PLAYER_CARD_PREFAB), playerNameContaier);
            TMP_Text playerNameField = UnityUtils.FindGameObject(playerNameCard, PLAYER_NAME_OBJ).GetComponent<TMP_Text>();
            playerNameField.text = playerNames[i];
        }

        teamRosterObjects.Add(teamCard);
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.RemoveListener(EventId.PlayerRosterUpdated, OnPlayerRosterUpdated);
    }
}
