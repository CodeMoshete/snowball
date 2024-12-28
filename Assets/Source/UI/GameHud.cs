using TMPro;
using UnityEngine;

public class GameHud : MonoBehaviour
{
    public TMP_Text AmmoCountField;
    public GameObject WallTooltip;

    private void Start()
    {
        Service.EventManager.AddListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.AddListener(EventId.WallPlacementBegin, OnWallPlacementBegin);
        Service.EventManager.AddListener(EventId.WallPlacementEnd, OnWallPlacementEnd);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        gameObject.SetActive(false);
    }

    private bool OnAmmoUpdated(object cookie)
    {
        int newCount = (int)cookie;
        AmmoCountField.text = newCount.ToString();
        return true;
    }

    private bool OnWallPlacementBegin(object cookie)
    {
        WallTooltip.SetActive(true);
        return true;
    }

    private bool OnWallPlacementEnd(object cookie)
    {
        WallTooltip.SetActive(false);
        return true;
    }

    private bool OnGameStateChanged(object cookie)
    {
        GameState gameState = (GameState)cookie;
        gameObject.SetActive(gameState == GameState.Gameplay);
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.RemoveListener(EventId.WallPlacementBegin, OnWallPlacementBegin);
        Service.EventManager.RemoveListener(EventId.WallPlacementEnd, OnWallPlacementEnd);
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
    }
}
