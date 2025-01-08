using TMPro;
using UnityEngine;

public class GameHud : MonoBehaviour
{
    public TMP_Text AmmoCountField;
    public GameObject TooltipObject;
    public TMP_Text TooltipText;
    public MobileHud MobileHud;

    private void Start()
    {
        Service.EventManager.AddListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.AddListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.AddListener(EventId.HideMessage, OnHideMessage);
        gameObject.SetActive(false);
    }

    private bool OnAmmoUpdated(object cookie)
    {
        int newCount = (int)cookie;
        AmmoCountField.text = newCount.ToString();
        return true;
    }

    private bool OnDisplayMessage(object cookie)
    {
        Debug.Log("Show tooltip");
        TooltipText.text = (string)cookie;
        TooltipObject.SetActive(true);
        return true;
    }

    private bool OnHideMessage(object cookie)
    {
        Debug.Log("Hide tooltip");
        TooltipObject.SetActive(false);
        return true;
    }

    private bool OnGameStateChanged(object cookie)
    {
        GameState gameState = (GameState)cookie;
        gameObject.SetActive(gameState == GameState.Gameplay);
#if UNITY_ANDROID || UNITY_IOS
        MobileHud.gameObject.SetActive(gameState == GameState.Gameplay);
#endif
        return false;
    }

    private void OnDestroy()
    {
        Service.EventManager.RemoveListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.RemoveListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.RemoveListener(EventId.HideMessage, OnHideMessage);
    }
}
