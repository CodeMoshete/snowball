using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public enum DownloadSource
{
    ProductionRemote,
    DebugResourcesLocal,
    DebugStreamingLocal
}

public class LevelLoader : MonoBehaviour
{
    private readonly string LOCAL_ASSET_PREFIX = $"file://{Application.streamingAssetsPath}/Levels";
    private const string REMOTE_ASSET_PREFIX = "https://www.codemoshete.com/snowball/levels";
    public const string LEVEL_MANIFEST_ASSET_PATH = "levels-manifest.json";
    public const string DEBUG_LEVEL_MANIFEST_ASSET_PATH = "debug-levels-manifest.json";
    // private const string REMOTE_ASSET_PREFIX = "https://codemoshete.s3.us-east-2.amazonaws.com/snowball/levels";

#if UNITY_STANDALONE_LINUX
    private const string PLATFORM_DIR = "StandaloneLinux64";
#elif UNITY_STANDALONE_WIN
    private const string PLATFORM_DIR = "StandaloneWindows";
#elif UNITY_STANDALONE_OSX
    private const string PLATFORM_DIR = "StandaloneOSX";
#elif UNITY_ANDROID
    private const string PLATFORM_DIR = "Android";
#elif UNITY_IOS
    private const string PLATFORM_DIR = "iOS";
#endif

    public DownloadSource DownloadSource = DownloadSource.ProductionRemote;

    private const string PREFAB_SUFFIX = ".prefab";

    private string levelName; // Name of the asset bundle (lowercase)
    private Action<GameObject> onDownloadSuccess;

    public static LevelLoader Instance
    {
        get; private set;
    }

    private void Start()
    {
        Instance = this;
    }

    public void LoadLevelManifest(Action<LevelManifestData> onDownloadSuccess, Action onDownloadFailure)
    {
        StartCoroutine(DownloadLevelsManifest(onDownloadSuccess, onDownloadFailure));
    }

    private IEnumerator DownloadLevelsManifest(Action<LevelManifestData> onDownloadSuccess, Action onDownloadFailure)
    {
        string urlPrefix;
        string manifestName;
        if (Constants.IS_OFFLINE_DEBUG || DownloadSource != DownloadSource.ProductionRemote)
        {
            urlPrefix = REMOTE_ASSET_PREFIX;
            manifestName = LEVEL_MANIFEST_ASSET_PATH;
        }
        else
        {
            urlPrefix = LOCAL_ASSET_PREFIX;
            manifestName = DEBUG_LEVEL_MANIFEST_ASSET_PATH;
        }

        string resourceUrl = $"{urlPrefix}/{manifestName}";
        Debug.Log($"Downloading {resourceUrl}");
        UnityWebRequest request = UnityWebRequest.Get(resourceUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"Asset download error: {request.error}");
            onDownloadFailure();
        }
        else {
            string jsonData = request.downloadHandler.text;
            LevelManifestData manifestData = JsonUtility.FromJson<LevelManifestData>(jsonData);
            onDownloadSuccess(manifestData);
        }
    }

    public void LoadLevel(string levelName, Action<GameObject> onDownloadSuccess, Action onDownloadFailure)
    {
        this.levelName = levelName.ToLower();
        this.onDownloadSuccess = onDownloadSuccess;

        // Load the asset bundle
        if (DownloadSource == DownloadSource.DebugResourcesLocal)
        {
            GameObject levelResource = Resources.Load<GameObject>($"Levels/{this.levelName}");
            if (levelResource == null)
            {
                Debug.LogError($"Level {this.levelName} not found in Resources.");
                onDownloadFailure();
            }
            else
            {
                Debug.Log($"Loaded level {this.levelName} from Resources.");
                onDownloadSuccess(levelResource);
            }
        }
        else
        {
            StartCoroutine(GetAssetBundle(this.levelName, OnBundleDownloadComplete, onDownloadFailure));
        }
    }

    private IEnumerator GetAssetBundle(string levelName, Action<AssetBundle> onComplete, Action onFail)
    {
        string urlPrefix = (Constants.IS_OFFLINE_DEBUG || DownloadSource == DownloadSource.DebugStreamingLocal) ? 
            LOCAL_ASSET_PREFIX : REMOTE_ASSET_PREFIX;

        string resourceUrl = $"{urlPrefix}/{PLATFORM_DIR}/{levelName}";
        Debug.Log($"Downloading level from {resourceUrl}");
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(resourceUrl);
        // UnityWebRequest www = UnityWebRequest.Get(resourceUrl);
        // www.SetRequestHeader("Accept", "*/*");
        // www.SetRequestHeader("User-Agent", "");
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"Asset download error: {www.error}");
            onFail();
        }
        else {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            onComplete(bundle);
        }
    }

    private void OnBundleDownloadComplete(AssetBundle bundle)
    {
        Debug.Log("Level AssetBundle downloaded successfully!");

        // Load the prefab
        string prefabName = string.Empty;
        string[] assetNames = bundle.GetAllAssetNames();
        for (int i = 0, count = assetNames.Length; i < count; ++i)
        {
            Debug.Log($"Checking asset named: {assetNames[i]}");
            if (assetNames[i].EndsWith(PREFAB_SUFFIX))
            {
                prefabName = assetNames[i];
                break;
            }
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError($"No prefabs found in level asset bundle for {levelName}");
            return;
        }

        GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
        bundle.Unload(false);

        if (prefab == null)
        {
            Debug.LogError($"Prefab {prefabName} not found in AssetBundle {levelName}.");
            return;
        }

        Debug.Log("Level loaded successfully!");
        onDownloadSuccess(prefab);
    }

    private void OnDestroy()
    {
        onDownloadSuccess = null;
    }
}
