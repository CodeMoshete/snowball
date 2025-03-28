using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    private const string HIT_NOTIFICATION_RESOURCE = "UI/PlayerHitNotification";
    private readonly Color GREEN_HIT_NOTIFICATION = new Color(0.41f, 1f, 0.57f);
    private readonly Color RED_HIT_NOTIFICATION = new Color(1f, 0.76f, 0.78f);
    private const float AMMO_INFO_SHOW_TIME = 3f;
    private const float AMMO_INFO_TRANSITION_TIME = 0.5f;
    public TMP_Text AmmoCountField;
    public CanvasGroup AmmoDescriptionContainer;
    public TMP_Text AmmoNameField;
    public TMP_Text AmmoDescriptionField;
    public GameObject TooltipObject;
    public TMP_Text TooltipText;
    public MobileHud MobileHud;
    public GameObject BuildingTooltipObj;
    public Image BuildingProgressBar;
    public GameObject DesktopControlsPromptPanel;
    public GameObject DesktopControlsPanel;
    public Image CurrentAmmoIcon;
    public Image IcyScreenImage;

    private float totalBuildTime;
    private float currentBuildTime;
    private bool isBuilding;
    private SnowballInventoryItem currentAmmo;
    private bool isShowingAmmoInfo;
    private float ammoInfoShowTime;

    private void Start()
    {
        Service.EventManager.AddListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.AddListener(EventId.AmmoTypeCycled, OnAmmoTypeUpdated);
        Service.EventManager.AddListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.AddListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.AddListener(EventId.HideMessage, OnHideMessage);
        Service.EventManager.AddListener(EventId.OnWallBuildStageStarted, OnWallBuilding);
        Service.EventManager.AddListener(EventId.PlayerFrozen, OnPlayerFrozen);
        Service.DataStreamManager.AddFloatDataStreamListener(FloatDataStream.PlayerHealth, OnPlayerHealthUpdated);
        gameObject.SetActive(false);
    }

    private bool OnAmmoTypeUpdated(object cookie)
    {
        currentAmmo = (SnowballInventoryItem)cookie;
        CurrentAmmoIcon.sprite = Resources.Load<Sprite>(currentAmmo.ThrowableObject.IconName);
        OnAmmoUpdated(currentAmmo);

        AmmoNameField.text = currentAmmo.ThrowableObject.DisplayName;
        AmmoDescriptionField.text = currentAmmo.ThrowableObject.Description;
        
        // TODO: Play reveal / hide animation.
        isShowingAmmoInfo = true;
        ammoInfoShowTime = AMMO_INFO_SHOW_TIME + 2f * AMMO_INFO_TRANSITION_TIME;
        AmmoDescriptionContainer.gameObject.SetActive(true);

        return false;
    }

    private bool OnAmmoUpdated(object cookie)
    {
        SnowballInventoryItem inventoryItem = (SnowballInventoryItem)cookie;

        // Set initial state.
        if (currentAmmo == null)
        {
            OnAmmoTypeUpdated(inventoryItem);
        }

        if (inventoryItem.ThrowableObject.Type == currentAmmo.ThrowableObject.Type)
        {
            AmmoCountField.text = inventoryItem.Quantity.ToString();
        }
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

    private void OnPlayerHealthUpdated(float health)
    {
        // Modify the float value _ThresholdBase on the material assigned to IcyScreenImage.
        float halfMaxHealth = Constants.MAX_HEALTH / 2f;
        float baseValue = 1f - Mathf.Min(health / halfMaxHealth, 1f);
        float opacityValue = 1f - Mathf.Max((health - halfMaxHealth) / halfMaxHealth, 0f);
        IcyScreenImage.material.SetFloat("_ThresholdBase", baseValue);
        IcyScreenImage.material.SetFloat("_ThresholdOpacity", opacityValue);
    }

    private bool OnPlayerFrozen(object cookie)
    {
        PlayerHitData hitData = (PlayerHitData)cookie;
        PlayerEntity throwingPlayer = hitData.ThrowingPlayer;
        string throwingPlayerName = throwingPlayer != null ? 
            throwingPlayer.PlayerName.Value.ToString() : 
            Constants.ENVIRONMENT_NAME;

        PlayerEntity hitPlayer = hitData.HitPlayer;

        GameObject notificationObj = Instantiate(Resources.Load<GameObject>(HIT_NOTIFICATION_RESOURCE), transform);
        PlayerHitNotification notification = notificationObj.GetComponent<PlayerHitNotification>();
        string notificationContent = string.Empty;
        Color notificationColor = Color.white;
        switch(hitData.Outcome)
        {
            case PlayerFrozenState.AllyFrozen:
                notificationContent = $"{hitPlayer} was frozen by {throwingPlayerName}!";
                notificationColor = RED_HIT_NOTIFICATION;
                break;
            case PlayerFrozenState.EnemyFrozen:
                notificationContent = $"{hitPlayer} was frozen by {throwingPlayerName}!";
                notificationColor = GREEN_HIT_NOTIFICATION;
                break;
            case PlayerFrozenState.AllyQueenFrozen:
                notificationContent = $"Your Queen was frozen by {throwingPlayerName}!";
                notificationColor = RED_HIT_NOTIFICATION;
                break;
            case PlayerFrozenState.LocalPlayerFrozeEnemy:
                notificationContent = $"You froze {hitPlayer}!";
                notificationColor = GREEN_HIT_NOTIFICATION;
                break;
            case PlayerFrozenState.LocalPlayerFrozeTeammate:
                notificationContent = $"You froze a teammate {hitPlayer}!";
                notificationColor = RED_HIT_NOTIFICATION;
                break;
            case PlayerFrozenState.LocalPlayerFrozen:
                notificationContent = $"You were frozen by {throwingPlayerName}!";
                notificationColor = RED_HIT_NOTIFICATION;
                break;
        }
        notification.ShowNotification(notificationContent, notificationColor);
        
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

        if (isShowingAmmoInfo)
        {
            ammoInfoShowTime -= Time.deltaTime;
            float fadeInTimeframe = AMMO_INFO_SHOW_TIME + AMMO_INFO_TRANSITION_TIME;
            if (ammoInfoShowTime > fadeInTimeframe)
            {
                AmmoDescriptionContainer.alpha = 1f - ((ammoInfoShowTime - fadeInTimeframe) / AMMO_INFO_TRANSITION_TIME);
            }
            else if (ammoInfoShowTime < AMMO_INFO_TRANSITION_TIME)
            {
                AmmoDescriptionContainer.alpha = ammoInfoShowTime / AMMO_INFO_TRANSITION_TIME;
            }
            else
            {
                AmmoDescriptionContainer.alpha = 1f;
            }

            if (ammoInfoShowTime <= 0f)
            {
                isShowingAmmoInfo = false;
                AmmoDescriptionContainer.gameObject.SetActive(false);
                ammoInfoShowTime = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        isBuilding = false;
        Service.EventManager.RemoveListener(EventId.AmmoUpdated, OnAmmoUpdated);
        Service.EventManager.RemoveListener(EventId.AmmoTypeCycled, OnAmmoTypeUpdated);
        Service.EventManager.RemoveListener(EventId.GameStateChanged, OnGameStateChanged);
        Service.EventManager.RemoveListener(EventId.DisplayMessage, OnDisplayMessage);
        Service.EventManager.RemoveListener(EventId.HideMessage, OnHideMessage);
        Service.EventManager.RemoveListener(EventId.OnWallBuildStageStarted, OnWallBuilding);
        Service.EventManager.RemoveListener(EventId.PlayerFrozen, OnPlayerFrozen);
        Service.DataStreamManager.RemoveFloatDataStreamListener(FloatDataStream.PlayerHealth, OnPlayerHealthUpdated);
    }
}
