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

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.AmmoUpdated, OnAmmoUpdated);
    }
}
