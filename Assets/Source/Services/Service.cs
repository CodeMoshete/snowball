public static class Service
{
    private static EventManager eventManager;
    public static EventManager EventManager
    {
        get
        {
            if (eventManager == null)
            {
                eventManager = new EventManager();
            }

            return eventManager;
        }
    }

    private static DataStreamManager dataStreamManager;
    public static DataStreamManager DataStreamManager
    {
        get
        {
            if (dataStreamManager == null)
            {
                dataStreamManager = new DataStreamManager();
            }

            return dataStreamManager;
        }
    }

    private static TimerManager timerMananager;
    public static TimerManager TimerManager
    {
        get
        {
            if (timerMananager == null)
            {
                timerMananager = new TimerManager();
            }

            return timerMananager;
        }
    }

    private static NetworkActionManager networkActions;
    public static NetworkActionManager NetworkActions
    {
        get
        {
            if (networkActions == null)
            {
                networkActions = new NetworkActionManager();
            }

            return networkActions;
        }
    }

    public static UpdateManager UpdateManager
    {
        get
        {
            return UpdateManager.Instance;
        }
    }

    public static LevelLoader LevelLoader
    {
        get
        {
            return LevelLoader.Instance;
        }
    }
}
