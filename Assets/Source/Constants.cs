using System.Diagnostics;
using UnityEngine;

public class Constants
{
    public const bool IS_OFFLINE_DEBUG = false;
    public const string SNOWBALL_PREFAB_NAME = "Snowball";
    public const string LOCAL_SNOWBALL_PREFAB_NAME = "ThrowableObjects/LocalSnowball";
    public const bool IS_FRIENDLY_FIRE_ON = true;
    public const int DEFAULT_START_AMMO = 3;
    public const int DEFAULT_WALL_COST = 0;
    public static int WallCost = DEFAULT_WALL_COST;
    public const float DEFAULT_WALL_BUILD_TIME = 3f;
    public static float WallBuildTime = DEFAULT_WALL_BUILD_TIME;
    public const bool DEFAULT_BUILDING_ENABLED = true;
    public static bool IsWallBuildingEnabled = DEFAULT_BUILDING_ENABLED;
    public const string TEAM_UNASSIGNED = "Unassigned";
    public const string PLAYER_NAME_DEFAULT = "Cahill";
    public const string ENVIRONMENT_NAME = "The Storm";
    public const float DEFAULT_MOVE_SPEED = 5f;
    public static float MoveSpeed = DEFAULT_MOVE_SPEED;
    public const string GAME_MANAGER_NAME = "GameManager(Clone)";
    public const float SNOWBALL_THROW_SPEED = 2400f;
    public const float MIN_THROW_ANGLE = 5f;
    public const float MAX_THROW_ANGLE = 25f;
    public const float BLIZZARD_TIMEOUT = 90f;
    public const string REMOTE_MANIFEST_URL = "https://www.codemoshete.com/snowball/levels/levels-manifest.json";
    private const string THROWABLES_RESOURCE = "ThrowableObjects/Throwables";
    public static Throwables SnowballTypes = Resources.Load<Throwables>(THROWABLES_RESOURCE);

    // Platform specific string values.
#if UNITY_STANDALONE_LINUX
    // Steam Deck
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press Y again to spawn wall\n({WallCost} Snowballs)";
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press F again to spawn wall\n({WallCost} Snowballs)";
#elif UNITY_ANDROID || UNITY_IOS
    public static string SNOWBALL_SPAWN_TOOLTIP_TEXT = $"Press Wall Button again to spawn wall\n({WallCost} Snowballs)";
#endif

    public static void ResetDefaultValues()
    {
        UnityEngine.Debug.Log("Resetting default values to Constants.");
        WallCost = DEFAULT_WALL_COST;
        WallBuildTime = DEFAULT_WALL_BUILD_TIME;
        IsWallBuildingEnabled = DEFAULT_BUILDING_ENABLED;
        MoveSpeed = DEFAULT_MOVE_SPEED;
    }
}
