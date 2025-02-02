using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    public TMP_Text AmmoCountField;
    public GameObject TooltipObject;
    public TMP_Text TooltipText;
    public MobileHud MobileHud;
    public GameObject BuildingTooltipObj;
    public Image BuildingProgressBar;

    private float totalBuildTime;
    private float currentBuildTime;
    private bool isBuilding;

    private void Start()
    {
        Service.EventManager.AddListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.AddListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.AddListener(EventId.HideMessage, OnHideMessage);
        Service.EventManager.AddListener(EventId.OnWallBuildStageStarted, OnWallBuilding);
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

    private bool OnWallBuilding(object cookie)
    {
        BuildingTooltipObj.SetActive(true);
        totalBuildTime = (float)cookie;
        currentBuildTime = 0f;
        BuildingProgressBar.fillAmount = 0f;
        isBuilding = true;
        return false;
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

    private void Update()
    {
        if (isBuilding)
        {
            currentBuildTime += Time.deltaTime;
            float pct = currentBuildTime / totalBuildTime;
            BuildingProgressBar.fillAmount = pct;
            if (pct >= 1f)
            {
                isBuilding = false;
                BuildingTooltipObj.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        isBuilding = false;
        Service.EventManager.RemoveListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.RemoveListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.RemoveListener(EventId.HideMessage, OnHideMessage);
        Service.EventManager.RemoveListener(EventId.OnWallBuildStageStarted, OnWallBuilding);
    }
}
