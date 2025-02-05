public class Constants
{
    public const bool IS_OFFLINE_DEBUG = false;
    public const string SNOWBALL_PREFAB_NAME = "Snowball";
    public const bool IS_FRIENDLY_FIRE_ON = true;
    public static int WallCost = 0;
    public static float WallBuildTime = 0f;
    public const string TEAM_UNASSIGNED = "Unassigned";
    public const string PLAYER_NAME_DEFAULT = "Cahill";
    public const string ENVIRONMENT_NAME = "The Storm";
    public static float MoveSpeed = 5f;
    public const string GAME_MANAGER_NAME = "GameManager(Clone)";
    public const float SNOWBALL_THROW_SPEED = 2400f;
    public const float MIN_THROW_ANGLE = 5f;
    public const float MAX_THROW_ANGLE = 25f;
    public const float BLIZZARD_TIMEOUT = 90f;
    public const string LEVEL_MANIFEST_ASSET_PATH = "Levels/levels-manifest.json";
    public const string REMOTE_MANIFEST_URL = "https://www.codemoshete.com/snowball/levels/levels-manifest.json";

    // Platform specific string values.
#if UNITY_STANDALONE_LINUX
    // Steam Deck
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press Y again to spawn wall\n({WallCost} Snowballs)";
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press F again to spawn wall\n({WallCost} Snowballs)";
#elif UNITY_ANDROID || UNITY_IOS
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press Wall Button again to spawn wall\n({WallCost} Snowballs)";
#endif
}
